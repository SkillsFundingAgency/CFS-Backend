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

namespace CalculateFunding.Services.Publishing
{
    public class RefreshService : IRefreshService
    {
        private readonly IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly ISpecificationService _specificationService;
        private readonly IProviderService _providerService;
        private readonly ICalculationResultsRepository _calculationResultsRepository;
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
        private readonly IPublishedProviderVersionService _publishedProviderVersionService;
        private readonly IPublishedProviderIndexerService _publishedProviderIndexerService;
        private readonly Policy _publishingResiliencePolicy;
        private readonly Policy _jobsApiClientPolicy;
        private readonly Policy _calculationsApiClientPolicy;

        public RefreshService(IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ISpecificationService specificationService,
            IProviderService providerService,
            ICalculationResultsRepository calculationResultsRepository,
            IPublishedProviderDataGenerator publishedProviderDataGenerator,
            IPublishedProviderContentsGeneratorResolver publishedProviderContentsGeneratorResolver,
            IProfilingService profilingService,
            IInScopePublishedProviderService inScopePublishedProviderService,
            IPublishedProviderDataPopulator publishedProviderDataPopulator,
            IJobsApiClient jobsApiClient,
            ILogger logger,
            IPublishedProviderVersionService publishedProviderVersionService,
            IPublishedProviderIndexerService publishedProviderIndexerService,
            ICalculationsApiClient calculationsApiClient,
            IPoliciesApiClient policiesApiClient,
            IRefreshPrerequisiteChecker refreshPrerequisiteChecker)
        {
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(calculationResultsRepository, nameof(calculationResultsRepository));
            Guard.ArgumentNotNull(publishedProviderDataGenerator, nameof(publishedProviderDataGenerator));
            Guard.ArgumentNotNull(publishedProviderContentsGeneratorResolver, nameof(publishedProviderContentsGeneratorResolver));
            Guard.ArgumentNotNull(inScopePublishedProviderService, nameof(inScopePublishedProviderService));
            Guard.ArgumentNotNull(publishedProviderDataPopulator, nameof(publishedProviderDataPopulator));
            Guard.ArgumentNotNull(profilingService, nameof(profilingService));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(publishedProviderVersionService, nameof(publishedProviderVersionService));
            Guard.ArgumentNotNull(publishedProviderIndexerService, nameof(publishedProviderIndexerService));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));

            _publishedProviderStatusUpdateService = publishedProviderStatusUpdateService;
            _publishedFundingRepository = publishedFundingRepository;
            _specificationService = specificationService;
            _providerService = providerService;
            _calculationResultsRepository = calculationResultsRepository;
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

            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _calculationsApiClientPolicy = publishingResiliencePolicies.CalculationsApiClient;
            _jobsApiClientPolicy = publishingResiliencePolicies.JobsApiClient;
            _publishedProviderVersionService = publishedProviderVersionService;
            _publishedProviderIndexerService = publishedProviderIndexerService;
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
            // Ignore this for now in the pr, its just place holder stuff for the next stories
            // We will be getting the job if from the message and the spec id
            // We will be adding telemtry
            // Updating cache with percentage comeplete
            // and whatever else

            Guard.ArgumentNotNull(message, nameof(message));

            Reference author = message.GetUserDetails();

            string specificationId = message.UserProperties["specificationId"] as string;
            string jobId = message.UserProperties["jobId"]?.ToString();

            JobViewModel currentJob = await RetrieveJobAndCheckCanBeProcessed(jobId);
            if (currentJob == null)
            {
                throw new NonRetriableException("Job can not be run");
            }

            // Update job to set status to processing
            await UpdateJobStatus(jobId, 0, 0, null, null);

            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);

            if (specification == null)
            {
                throw new NonRetriableException($"Could not find specification with id '{specificationId}'");
            }

            // Check prerequisites for this specification to be chosen/refreshed
            IEnumerable<string> prereqValidationErrors = await _refreshPrerequisiteChecker.PerformPrerequisiteChecks(specification);
            if (!prereqValidationErrors.IsNullOrEmpty())
            {
                string errorMessage = $"Specification with id: '{specificationId} has prerequisites which aren't complete.";

                await UpdateJobStatus(jobId, completedSuccessfully: false, outcome: string.Join(", ", prereqValidationErrors));

                throw new NonRetriableException(errorMessage);
            }

            // Get scoped providers for this specification
            IEnumerable<Common.ApiClient.Providers.Models.Provider> scopedProvidersResponse = await _providerService.GetProvidersByProviderVersionsId(specification.ProviderVersionId);

            Dictionary<string, Common.ApiClient.Providers.Models.Provider> scopedProviders = new Dictionary<string, Common.ApiClient.Providers.Models.Provider>();
            foreach (Common.ApiClient.Providers.Models.Provider provider in scopedProvidersResponse)
            {
                scopedProviders.Add(provider.ProviderId, provider);
            }

            // TODO: update job with number of providers * number of funding streams
            //             await UpdateJobStatus(jobId, TOTAL NUMBER OF ITEMS, 0, null, null);

            // Get existing published providers for this specification
            // TODO: Change to lookup per fundingstream for the specification to reduce load time and memory
            IEnumerable<PublishedProvider> existingPublishedProviders = await _publishingResiliencePolicy.ExecuteAsync(() =>
                _publishedFundingRepository.GetLatestPublishedProvidersBySpecification(specificationId));

            if (existingPublishedProviders.IsNullOrEmpty())
                throw new RetriableException(
                    $"Null or empty publsihed providers returned for specification id : '{specificationId}' when setting status to updated");

            // Get calculation results for specification
            IEnumerable<ProviderCalculationResult> allCalculationResults = await _calculationResultsRepository.GetCalculationResultsBySpecificationId(specificationId);
            Dictionary<string, IEnumerable<CalculationResult>> calculationResults = new Dictionary<string, IEnumerable<CalculationResult>>();
            foreach (var result in allCalculationResults)
            {
                calculationResults.Add(result.ProviderId, result.Results);
            }

            foreach (Reference fundingStream in specification.FundingStreams)
            {
                Dictionary<string, PublishedProvider> publishedProvidersToUpdate = new Dictionary<string, PublishedProvider>();

                ApiResponse<TemplateMetadataContents> templateMetadataContentsResponse = await _policiesApiClient.GetFundingTemplateContents(fundingStream.Id, specification.TemplateIds[fundingStream.Id]);
                // TODO: Null and response checking on response. If there is a null associated template, continue to next funding stream
                TemplateMetadataContents templateMetadataContents = templateMetadataContentsResponse.Content;

                Dictionary<string, PublishedProvider> publishedProviders = new Dictionary<string, PublishedProvider>();
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

                // Generate populated data for each provider in this funding line
                Dictionary<string, GeneratedProviderResult> generatedPublishedProviderData = _publishedProviderDataGenerator.Generate(templateMetadataContents, templateMapping, scopedProviders.Values, allCalculationResults);

                Dictionary<string, IEnumerable<Models.Publishing.FundingLine>> fundingLinesForProfiling = new Dictionary<string, IEnumerable<Models.Publishing.FundingLine>>(generatedPublishedProviderData.Select(c => new KeyValuePair<string, IEnumerable<Models.Publishing.FundingLine>>(c.Key, c.Value.FundingLines.Where(f => f.Type == OrganisationGroupingReason.Payment))));

                // Profile payment funding lines
                await _profilingService.ProfileFundingLines(fundingLinesForProfiling, fundingStream.Id, specification.FundingPeriod.Id);

                // Set generated data on the Published provider
                foreach (KeyValuePair<string, PublishedProvider> publishedProvider in publishedProviders)
                {
                    PublishedProviderVersion publishedProviderVersion = publishedProvider.Value.Current;
                    string providerId = publishedProviderVersion.ProviderId;
                    
                    bool publishedProviderUpdated = _publishedProviderDataPopulator.UpdatePublishedProvider(publishedProviderVersion, 
                        generatedPublishedProviderData[publishedProvider.Key], 
                        scopedProviders[providerId],
                        specification.TemplateIds[providerId]);

                    if (publishedProviderUpdated)
                    {
                        publishedProvidersToUpdate.Add(publishedProvider.Key, publishedProvider.Value);
                    }
                }

                if (publishedProvidersToUpdate.Any())
                {
                    // Save updated PublishedProviders to cosmos and increment version status
                    await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProvidersToUpdate.Values, author, PublishedProviderStatus.Updated);

                    // Generate contents JSON for provider and save to blob storage
                    IPublishedProviderContentsGenerator generator = _publishedProviderContentsGeneratorResolver.GetService(templateMetadataContents.SchemaVersion);
                    foreach (KeyValuePair<string, PublishedProvider> provider in publishedProvidersToUpdate)
                    {
                        PublishedProviderVersion publishedProviderVersion = provider.Value.Current;

                        string contents = generator.GenerateContents(publishedProviderVersion, templateMetadataContents, templateMapping, generatedPublishedProviderData[provider.Key]);

                        if (string.IsNullOrWhiteSpace(contents))
                        {
                            throw new RetriableException($"Generator failed to generate content for published provider version with id: '{publishedProviderVersion.Id}'");
                        }

                        try
                        {
                            await _publishedProviderVersionService.SavePublishedProviderVersionBody(publishedProviderVersion.Id, contents);
                        }
                        catch (Exception ex)
                        {
                            throw new RetriableException(ex.Message);
                        }

                        try
                        {
                            await _publishedProviderIndexerService.IndexPublishedProvider(publishedProviderVersion);
                        }
                        catch (Exception ex)
                        {
                            throw new RetriableException(ex.Message);
                        }
                    }
                }
            }

            // Mark job as complete
            await UpdateJobStatus(jobId, 0, 0, true, null);
        }

        private async Task<JobViewModel> RetrieveJobAndCheckCanBeProcessed(string jobId)
        {
            ApiResponse<JobViewModel> response = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.GetJobById(jobId));

            if (response == null || response.Content == null)
            {
                _logger.Error($"Could not find the job with id: '{jobId}'");
                return null;
            }

            JobViewModel job = response.Content;

            if (job.CompletionStatus.HasValue)
            {
                _logger.Information($"Received job with id: '{jobId}' is already in a completed state with status {job.CompletionStatus.ToString()}");
                return null;
            }

            return job;
        }

        private async Task UpdateJobStatus(string jobId, int percentComplete = 0, bool? completedSuccessfully = null, string outcome = null)
        {
            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = completedSuccessfully,
                ItemsProcessed = percentComplete,
                Outcome = outcome
            };

            ApiResponse<JobLog> jobLogResponse = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(jobId, jobLogUpdateModel));

            if (jobLogResponse == null || jobLogResponse.Content == null)
            {
                _logger.Error($"Failed to add a job log for job id '{jobId}'");
            }
        }

        private async Task UpdateJobStatus(string jobId, int totalItemsCount, int failedItemsCount, bool? completedSuccessfully = null, string outcome = null)
        {
            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = completedSuccessfully,
                ItemsProcessed = totalItemsCount,
                ItemsFailed = failedItemsCount,
                ItemsSucceeded = totalItemsCount - failedItemsCount,
                Outcome = outcome
            };

            ApiResponse<JobLog> jobLogResponse = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(jobId, jobLogUpdateModel));

            if (jobLogResponse == null || jobLogResponse.Content == null)
            {
                _logger.Error($"Failed to add a job log for job id '{jobId}'");
            }
        }
    }
}