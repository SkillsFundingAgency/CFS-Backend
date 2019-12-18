using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Publishing
{
    public class RefreshService : IRefreshService
    {
        private readonly IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly ISpecificationService _specificationService;
        private readonly IProviderService _providerService;
        private readonly ICalculationResultsService _calculationResultsService;
        private readonly IPublishedProviderDataGenerator _publishedProviderDataGenerator;
        private readonly IPublishedProviderContentsGeneratorResolver _publishedProviderContentsGeneratorResolver;
        private readonly IInScopePublishedProviderService _inScopePublishedProviderService;
        private readonly IPublishedProviderDataPopulator _publishedProviderDataPopulator;
        private readonly IProfilingService _profilingService;
        private readonly IJobsApiClient _jobsApiClient;
        private readonly ILogger _logger;
        private readonly ICalculationsApiClient _calculationsApiClient;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly IRefreshPrerequisiteChecker _refreshPrerequisiteChecker;
        private readonly Policy _publishingResiliencePolicy;
        private readonly Policy _jobsApiClientPolicy;
        private readonly Policy _calculationsApiClientPolicy;
        private readonly IPublishProviderExclusionCheck _providerExclusionCheck;
        private readonly IFundingLineValueOverride _fundingLineValueOverride;
        private readonly IPublishedProviderIndexerService _publishedProviderIndexerService;
        private readonly IJobManagement _jobManagement;
        private readonly IPublishingFeatureFlag _publishingFeatureFlag;

        public RefreshService(IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IPublishedFundingDataService publishedFundingDataService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ISpecificationService specificationService,
            IProviderService providerService,
            ICalculationResultsService calculationResultsService,
            IPublishedProviderDataGenerator publishedProviderDataGenerator,
            IPublishedProviderContentsGeneratorResolver publishedProviderContentsGeneratorResolver,
            IProfilingService profilingService,
            IInScopePublishedProviderService inScopePublishedProviderService,
            IPublishedProviderDataPopulator publishedProviderDataPopulator,
            IJobsApiClient jobsApiClient,
            ILogger logger,
            ICalculationsApiClient calculationsApiClient,
            IPoliciesApiClient policiesApiClient,
            IRefreshPrerequisiteChecker refreshPrerequisiteChecker,
            IPublishProviderExclusionCheck providerExclusionCheck,
            IFundingLineValueOverride fundingLineValueOverride,
            IJobManagement jobManagement,
            IPublishingFeatureFlag publishingFeatureFlag,
            IPublishedProviderIndexerService publishedProviderIndexerService)
        {
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(calculationResultsService, nameof(calculationResultsService));
            Guard.ArgumentNotNull(publishedProviderDataGenerator, nameof(publishedProviderDataGenerator));
            Guard.ArgumentNotNull(publishedProviderContentsGeneratorResolver, nameof(publishedProviderContentsGeneratorResolver));
            Guard.ArgumentNotNull(inScopePublishedProviderService, nameof(inScopePublishedProviderService));
            Guard.ArgumentNotNull(publishedProviderDataPopulator, nameof(publishedProviderDataPopulator));
            Guard.ArgumentNotNull(profilingService, nameof(profilingService));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(providerExclusionCheck, nameof(providerExclusionCheck));
            Guard.ArgumentNotNull(fundingLineValueOverride, nameof(fundingLineValueOverride));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(publishingFeatureFlag, nameof(publishingFeatureFlag));
            Guard.ArgumentNotNull(publishedProviderIndexerService, nameof(publishedProviderIndexerService));

            _publishedProviderStatusUpdateService = publishedProviderStatusUpdateService;
            _publishedFundingDataService = publishedFundingDataService;
            _specificationService = specificationService;
            _providerService = providerService;
            _calculationResultsService = calculationResultsService;
            _publishedProviderDataGenerator = publishedProviderDataGenerator;
            _publishedProviderContentsGeneratorResolver = publishedProviderContentsGeneratorResolver;
            _inScopePublishedProviderService = inScopePublishedProviderService;
            _publishedProviderDataPopulator = publishedProviderDataPopulator;
            _profilingService = profilingService;
            _jobsApiClient = jobsApiClient;
            _logger = logger;
            _calculationsApiClient = calculationsApiClient;
            _policiesApiClient = policiesApiClient;
            _refreshPrerequisiteChecker = refreshPrerequisiteChecker;
            _providerExclusionCheck = providerExclusionCheck;
            _fundingLineValueOverride = fundingLineValueOverride;
            _publishedProviderIndexerService = publishedProviderIndexerService;

            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _calculationsApiClientPolicy = publishingResiliencePolicies.CalculationsApiClient;
            _jobsApiClientPolicy = publishingResiliencePolicies.JobsApiClient;
            _jobManagement = jobManagement;
            _publishingFeatureFlag = publishingFeatureFlag;
        }

        public async Task<IEnumerable<Common.ApiClient.Providers.Models.Provider>> GetProvidersByProviderVersionId(string providerVersionId)
        {
            return await _providerService.GetProvidersByProviderVersionsId(providerVersionId);
        }

        public async Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId)
        {
            return await _specificationService.GetSpecificationSummaryById(specificationId);
        }

        public async Task RefreshResults(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            Reference author = message.GetUserDetails();

            string specificationId = message.UserProperties["specification-id"] as string;
            string jobId = message.UserProperties["jobId"]?.ToString();

            JobViewModel currentJob;
            try
            {
                currentJob = await _jobManagement.RetrieveJobAndCheckCanBeProcessed(jobId);
            }
            catch (Exception e)
            {
                throw new NonRetriableException($"Job cannot be run. {e.Message}");
            }

            // Update job to set status to processing
            await _jobManagement.UpdateJobStatus(jobId, 0, 0, null, null);

            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);

            if (specification == null)
            {
                throw new NonRetriableException($"Could not find specification with id '{specificationId}'");
            }

            _logger.Information($"Verifying prerequisites for funding refresh");

            await CheckPrerequisitesForSpecificationToBeRefreshed(specification, jobId);

            _logger.Information($"Prerequisites for refresh passed");

            // Get scoped providers for this specification
            IEnumerable<Common.ApiClient.Providers.Models.Provider> scopedProvidersResponse = await _providerService.GetScopedProvidersForSpecification(specification.Id, specification.ProviderVersionId);

            Dictionary<string, Common.ApiClient.Providers.Models.Provider> scopedProviders = new Dictionary<string, Common.ApiClient.Providers.Models.Provider>();
            foreach (Common.ApiClient.Providers.Models.Provider provider in scopedProvidersResponse)
            {
                scopedProviders.Add(provider.ProviderId, provider);
            }

            _logger.Information($"Found {scopedProviders.Count} scoped providers for refresh");

            // Get calculation results for specification 
            _logger.Information($"Looking up calculation results");

            IDictionary<string, ProviderCalculationResult> allCalculationResults;
            try
            {
                allCalculationResults = await _calculationResultsService.GetCalculationResultsBySpecificationId(specificationId, scopedProviders.Keys);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception during calculation result lookup");
                throw;
            }

            _logger.Information($"Found calculation results for {allCalculationResults.Count} providers from cosmos for refresh job");

            foreach (Reference fundingStream in specification.FundingStreams)
            {
                ApiResponse<TemplateMetadataContents> templateMetadataContentsResponse = await _policiesApiClient.GetFundingTemplateContents(fundingStream.Id, specification.TemplateIds[fundingStream.Id]);
                // TODO: Null and response checking on response. If there is a null associated template, continue to next funding stream
                TemplateMetadataContents templateMetadataContents = templateMetadataContentsResponse.Content;

                Dictionary<string, PublishedProvider> publishedProviders = new Dictionary<string, PublishedProvider>();

                // Get existing published providers for this specification
                _logger.Information("Looking up existing published providers from cosmos for refresh job");
                List<PublishedProvider> existingPublishedProviders = (await _publishingResiliencePolicy.ExecuteAsync(() =>
                    _publishedFundingDataService.GetCurrentPublishedProviders(fundingStream.Id, specification.FundingPeriod.Id))).ToList();
                _logger.Information($"Found {existingPublishedProviders.Count} existing published providers from cosmos for refresh job");

                foreach (PublishedProvider publishedProvider in existingPublishedProviders)
                {
                    if (publishedProvider.Current.FundingStreamId == fundingStream.Id)
                    {
                        publishedProviders.Add(publishedProvider.Current.ProviderId, publishedProvider);
                    }
                }

                // Create PublishedProvider for providers which don't already have a record (eg ProviderID-FundingStreamId-FundingPeriodId)
                Dictionary<string, PublishedProvider> newProviders = _inScopePublishedProviderService.GenerateMissingProviders(scopedProviders.Values, specification, fundingStream, publishedProviders, templateMetadataContents);
                publishedProviders.AddRange(newProviders);

                // Get TemplateMapping for calcs from Calcs API client nuget
                ApiResponse<Common.ApiClient.Calcs.Models.TemplateMapping> calculationMappingResult = await _calculationsApiClientPolicy.ExecuteAsync(() => _calculationsApiClient.GetTemplateMapping(specificationId, fundingStream.Id));
                if (calculationMappingResult == null)
                {
                    throw new Exception($"calculationMappingResult returned null for funding stream {fundingStream.Id}");
                }

                Common.ApiClient.Calcs.Models.TemplateMapping templateMapping = calculationMappingResult.Content;

                _logger.Information("Generating PublishedProviders for refresh");

                // Generate populated data for each provider in this funding line
                Dictionary<string, GeneratedProviderResult> generatedPublishedProviderData;
                try
                {
                    generatedPublishedProviderData = _publishedProviderDataGenerator.Generate(templateMetadataContents, templateMapping, scopedProviders.Values, allCalculationResults);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Exception during generating provider data");
                    throw;
                }
                _logger.Information("Populated PublishedProviders for refresh");

                Dictionary<string, PublishedProvider> publishedProvidersToUpdate = new Dictionary<string, PublishedProvider>();

                Dictionary<string, PublishedProvider> existingPublishedProvidersToUpdate = new Dictionary<string, PublishedProvider>();

                FundingLine[] flattenedTemplateFundingLines = templateMetadataContents.RootFundingLines.Flatten(_ => _.FundingLines).ToArray();
                Calculation[] flattedCalculations = flattenedTemplateFundingLines.SelectMany(_ => _.Calculations.Flatten(c => c.Calculations)).ToArray();

                _logger.Information("Profiling providers for refresh");

                // Profile payment funding lines
                try
                {
                    await _profilingService.ProfileFundingLines(generatedPublishedProviderData.SelectMany(c => c.Value.FundingLines.Where(f => f.Type == OrganisationGroupingReason.Payment)), fundingStream.Id, specification.FundingPeriod.Id);
                }
                catch (Exception ex)
                {

                    _logger.Error(ex, "Exception during generating provider profiling");
                    throw;
                }

                _logger.Information("Finished profiling providers for refresh");

                // Set generated data on the Published provider
                foreach (KeyValuePair<string, PublishedProvider> publishedProvider in publishedProviders)
                {
                    PublishedProviderVersion publishedProviderVersion = publishedProvider.Value.Current;
                    string providerId = publishedProviderVersion.ProviderId;

                    GeneratedProviderResult generatedProviderResult = generatedPublishedProviderData[publishedProvider.Key];

                    PublishedProviderExclusionCheckResult exclusionCheckResult =
                        _providerExclusionCheck.ShouldBeExcluded(allCalculationResults[providerId], templateMapping, flattedCalculations);

                    if (exclusionCheckResult.ShouldBeExcluded)
                    {
                        if (newProviders.ContainsKey(publishedProvider.Key))
                        {
                            newProviders.Remove(publishedProvider.Key);
                        }

                        if (!_fundingLineValueOverride.TryOverridePreviousFundingLineValues(publishedProviderVersion, generatedProviderResult))
                        {
                            //there are no none null payment funding line values and we didn't have to override any previous
                            //version funding lines with a zero amount now they are all null so skip this published provider 
                            //the updates check

                            continue;
                        }
                    }

                    bool publishedProviderUpdated = _publishedProviderDataPopulator.UpdatePublishedProvider(publishedProviderVersion,
                        generatedProviderResult,
                        scopedProviders[providerId],
                        specification.TemplateIds[fundingStream.Id]);

                    if (publishedProviderUpdated)
                    {
                        if (!newProviders.Contains(publishedProvider))
                        {
                            existingPublishedProvidersToUpdate.Add(publishedProvider.Key, publishedProvider.Value);
                        }

                        publishedProvidersToUpdate.Add(publishedProvider.Key, publishedProvider.Value);
                    }
                }

                _logger.Information($"Updating a total of {publishedProvidersToUpdate.Count} published providers");

                if (publishedProvidersToUpdate.Any())
                {
                    // Save updated PublishedProviders to cosmos and increment version status
                    if (existingPublishedProvidersToUpdate.Any())
                    {
                        _logger.Information($"Saving updates to existing published providers. Total={existingPublishedProvidersToUpdate.Count}");
                        await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(existingPublishedProvidersToUpdate.Values, author, PublishedProviderStatus.Updated, jobId);

                        _logger.Information($"Indexing existing PublishedProviders");
                        await _publishedProviderIndexerService.IndexPublishedProviders(existingPublishedProvidersToUpdate.Values.Select(_ => _.Current));
                    }

                    if (newProviders.Any())
                    {
                        _logger.Information($"Saving new published providers. Total={newProviders.Count}");
                        await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(newProviders.Values, author, PublishedProviderStatus.Draft, jobId);

                        _logger.Information($"Indexing newly added PublishedProviders");
                        await _publishedProviderIndexerService.IndexPublishedProviders(newProviders.Values.Select(_ => _.Current));
                    }
                }
            }

            _logger.Information("Marking job as complete");
            // Mark job as complete
            await _jobManagement.UpdateJobStatus(jobId, 0, 0, true, null);
            _logger.Information("Refresh complete");
        }

        private async Task CheckPrerequisitesForSpecificationToBeRefreshed(SpecificationSummary specification, string jobId)
        {
            // Check prerequisites for this specification to be chosen/refreshed
            IEnumerable<string> prereqValidationErrors = await _refreshPrerequisiteChecker.PerformPrerequisiteChecks(specification);
            if (!prereqValidationErrors.IsNullOrEmpty())
            {
                string errorMessage = $"Specification with id: '{specification.Id} has prerequisites which aren't complete.";

                await _jobManagement.UpdateJobStatus(jobId, completedSuccessfully: false, outcome: string.Join(", ", prereqValidationErrors));

                throw new NonRetriableException(errorMessage);
            }
        }
    }
}