using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Publishing
{
    public class RefreshService : IRefreshService
    {
        private const string SfaCorrelationId = "sfa-correlationId";
        
        private readonly IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly ISpecificationService _specificationService;
        private readonly IProviderService _providerService;
        private readonly ICalculationResultsService _calculationResultsService;
        private readonly IPublishedProviderDataGenerator _publishedProviderDataGenerator;
        private readonly IPublishedProviderDataPopulator _publishedProviderDataPopulator;
        private readonly IProfilingService _profilingService;
        private readonly ILogger _logger;
        private readonly ICalculationsApiClient _calculationsApiClient;
        private readonly IPrerequisiteCheckerLocator _prerequisiteCheckerLocator;
        private readonly AsyncPolicy _publishingResiliencePolicy;
        private readonly AsyncPolicy _calculationsApiClientPolicy;
        private readonly IPublishProviderExclusionCheck _providerExclusionCheck;
        private readonly IFundingLineValueOverride _fundingLineValueOverride;
        private readonly IPublishedProviderIndexerService _publishedProviderIndexerService;
        private readonly IJobManagement _jobManagement;
        private readonly IGeneratePublishedFundingCsvJobsCreationLocator _generateCsvJobsLocator;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IPublishedProviderVersionService _publishedProviderVersionService;
        private readonly IPoliciesService _policiesService;
        private readonly IVariationService _variationService;
        private readonly IReApplyCustomProfiles _reApplyCustomProfiles;

        public RefreshService(IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IPublishedFundingDataService publishedFundingDataService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ISpecificationService specificationService,
            IProviderService providerService,
            ICalculationResultsService calculationResultsService,
            IPublishedProviderDataGenerator publishedProviderDataGenerator,
            IProfilingService profilingService,
            IPublishedProviderDataPopulator publishedProviderDataPopulator,
            ILogger logger,
            ICalculationsApiClient calculationsApiClient,
            IPrerequisiteCheckerLocator prerequisiteCheckerLocator,
            IPublishProviderExclusionCheck providerExclusionCheck,
            IFundingLineValueOverride fundingLineValueOverride,
            IJobManagement jobManagement,
            IPublishedProviderIndexerService publishedProviderIndexerService,
            IVariationService variationService,
            ITransactionFactory transactionFactory,
            IPublishedProviderVersionService publishedProviderVersionService,
            IPoliciesService policiesService,
            IGeneratePublishedFundingCsvJobsCreationLocator generateCsvJobsLocator, 
            IReApplyCustomProfiles reApplyCustomProfiles)
        {
            Guard.ArgumentNotNull(generateCsvJobsLocator, nameof(generateCsvJobsLocator));
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(calculationResultsService, nameof(calculationResultsService));
            Guard.ArgumentNotNull(publishedProviderDataGenerator, nameof(publishedProviderDataGenerator));
            Guard.ArgumentNotNull(publishedProviderDataPopulator, nameof(publishedProviderDataPopulator));
            Guard.ArgumentNotNull(profilingService, nameof(profilingService));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(providerExclusionCheck, nameof(providerExclusionCheck));
            Guard.ArgumentNotNull(fundingLineValueOverride, nameof(fundingLineValueOverride));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(publishedProviderIndexerService, nameof(publishedProviderIndexerService));
            Guard.ArgumentNotNull(variationService, nameof(variationService));
            Guard.ArgumentNotNull(transactionFactory, nameof(transactionFactory));
            Guard.ArgumentNotNull(publishedProviderVersionService, nameof(publishedProviderVersionService));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(prerequisiteCheckerLocator, nameof(prerequisiteCheckerLocator));
            Guard.ArgumentNotNull(reApplyCustomProfiles, nameof(reApplyCustomProfiles));

            _publishedProviderStatusUpdateService = publishedProviderStatusUpdateService;
            _publishedFundingDataService = publishedFundingDataService;
            _specificationService = specificationService;
            _providerService = providerService;
            _calculationResultsService = calculationResultsService;
            _publishedProviderDataGenerator = publishedProviderDataGenerator;
            _publishedProviderDataPopulator = publishedProviderDataPopulator;
            _profilingService = profilingService;
            _logger = logger;
            _calculationsApiClient = calculationsApiClient;
            _prerequisiteCheckerLocator = prerequisiteCheckerLocator;
            _providerExclusionCheck = providerExclusionCheck;
            _fundingLineValueOverride = fundingLineValueOverride;
            _variationService = variationService;
            _generateCsvJobsLocator = generateCsvJobsLocator;
            _reApplyCustomProfiles = reApplyCustomProfiles;
            _publishedProviderIndexerService = publishedProviderIndexerService;

            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _calculationsApiClientPolicy = publishingResiliencePolicies.CalculationsApiClient;
            _jobManagement = jobManagement;
            _transactionFactory = transactionFactory;
            _publishedProviderVersionService = publishedProviderVersionService;
            _policiesService = policiesService;
        }

        public async Task RefreshResults(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));
            
            Reference author = message.GetUserDetails();

            string specificationId = message.UserProperties["specification-id"] as string;
            string jobId = message.UserProperties["jobId"]?.ToString();

            try
            {
                await _jobManagement.RetrieveJobAndCheckCanBeProcessed(jobId);
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

            // Check prerequisites for this specification to be chosen/refreshed
            IPrerequisiteChecker prerequisiteChecker = _prerequisiteCheckerLocator.GetPreReqChecker(PrerequisiteCheckerType.Refresh);
            await prerequisiteChecker.PerformChecks(specification, jobId);

            _logger.Information($"Prerequisites for refresh passed");

            // Get scoped providers for this specification
            IDictionary<string, Provider> scopedProviders = await _providerService.GetScopedProvidersForSpecification(specification.Id, specification.ProviderVersionId);

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

            _logger.Information($"Found calculation results for {allCalculationResults?.Count} providers from cosmos for refresh job");

            string correlationId = message.GetUserProperty<string>(SfaCorrelationId);

            try
            {
                foreach (Reference fundingStream in specification.FundingStreams)
                {
                    await RefreshFundingStream(fundingStream, specification, scopedProviders, allCalculationResults, jobId, author, correlationId);
                }
            }
            finally
            {
                _variationService.ClearSnapshots();
            }

            _logger.Information("Marking job as complete");
            // Mark job as complete
            await _jobManagement.UpdateJobStatus(jobId, 0, 0, true, null);
            _logger.Information("Refresh complete");
        }

        private async Task RefreshFundingStream(Reference fundingStream, SpecificationSummary specification, IDictionary<string, Provider> scopedProviders, IDictionary<string, ProviderCalculationResult> allCalculationResults, string jobId, Reference author, string correlationId)
        {
            TemplateMetadataContents templateMetadataContents = await _policiesService.GetTemplateMetadataContents(fundingStream.Id, specification.FundingPeriod.Id, specification.TemplateIds[fundingStream.Id]);
            
            if (templateMetadataContents == null)
            {
                _logger.Information($"Unable to locate template meta data contents for funding stream:'{fundingStream.Id}' and template id:'{specification.TemplateIds[fundingStream.Id]}'");
                return;
            }

            Dictionary<string, PublishedProvider> publishedProviders = new Dictionary<string, PublishedProvider>();
            
            // Get existing published providers for this specification
            _logger.Information("Looking up existing published providers from cosmos for refresh job");

            string fundingPeriodId = await _policiesService.GetFundingPeriodId(specification.FundingPeriod.Id);

            List<PublishedProvider> existingPublishedProviders = (await _publishingResiliencePolicy.ExecuteAsync(() =>
                _publishedFundingDataService.GetCurrentPublishedProviders(fundingStream.Id, fundingPeriodId))).ToList();
            _logger.Information($"Found {existingPublishedProviders.Count} existing published providers from cosmos for refresh job");

            foreach (PublishedProvider publishedProvider in existingPublishedProviders)
            {
                if (publishedProvider.Current.FundingStreamId == fundingStream.Id)
                {
                    publishedProviders.Add(publishedProvider.Current.ProviderId, publishedProvider);
                }
            }

            // Create PublishedProvider for providers which don't already have a record (eg ProviderID-FundingStreamId-FundingPeriodId)
            IDictionary<string, PublishedProvider> newProviders = await _providerService.GenerateMissingPublishedProviders(scopedProviders.Values, specification, fundingStream, publishedProviders);
            publishedProviders.AddRange(newProviders);

            // Get TemplateMapping for calcs from Calcs API client nuget
            ApiResponse<Common.ApiClient.Calcs.Models.TemplateMapping> calculationMappingResult = await _calculationsApiClientPolicy.ExecuteAsync(() => _calculationsApiClient.GetTemplateMapping(specification.Id, fundingStream.Id));
            if (calculationMappingResult == null)
            {
                throw new Exception($"calculationMappingResult returned null for funding stream {fundingStream.Id}");
            }

            Common.ApiClient.Calcs.Models.TemplateMapping templateMapping = calculationMappingResult.Content;

            _logger.Information("Generating PublishedProviders for refresh");

            // Generate populated data for each provider in this funding line
            IDictionary<string, GeneratedProviderResult> generatedPublishedProviderData;
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
            
            try
            {
                await _profilingService.ProfileFundingLines(generatedPublishedProviderData.SelectMany(c => 
                    c.Value.FundingLines.Where(f => f.Type == OrganisationGroupingReason.Payment)), 
                    fundingStream.Id, 
                    fundingPeriodId);
            }
            catch (Exception ex)
            {

                _logger.Error(ex, "Exception during generating provider profiling");
                
                throw;
            }

            _logger.Information("Finished profiling providers for refresh");

            // snapshot the current published providers so any changes aren't reflected when we detect variations later
            await _variationService.SnapShot(publishedProviders, fundingStream.Id);

            //we need enumerate a readonly cut of this as we add to it in some variations now (for missing providers not in scope)
            Dictionary<string, PublishedProvider> publishedProvidersReadonlyDictionary = publishedProviders.ToDictionary(_ => _.Key, _ => _.Value);

            foreach (KeyValuePair<string, PublishedProvider> publishedProvider in publishedProvidersReadonlyDictionary)
            {
                PublishedProviderVersion publishedProviderVersion = publishedProvider.Value.Current;
                
                string providerId = publishedProviderVersion.ProviderId;

                // this could be a retry and the key may not exist as the provider has been created as a successor so we need to skip
                if(!generatedPublishedProviderData.ContainsKey(publishedProvider.Key))
                {
                    continue;
                }

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
                    specification.TemplateIds[fundingStream.Id],
                    newProviders.ContainsKey(publishedProvider.Key));

                //reapply any custom profiles this provider has and internally check for errors
                await _reApplyCustomProfiles.ProcessPublishedProvider(publishedProviderVersion);

                if (publishedProviderUpdated && existingPublishedProviders.AnyWithNullCheck())
                {
                    FundingConfiguration fundingConfiguration = await _policiesService.GetFundingConfiguration(fundingStream.Id, specification.FundingPeriod.Id);

                    IDictionary<string, PublishedProvider> newPublishedProviders = await _variationService.PrepareVariedProviders(generatedProviderResult.TotalFunding,
                        publishedProviders,
                        publishedProvider.Value,
                        scopedProviders[providerId],
                        fundingConfiguration?.Variations,
                        fundingStream.Id,
                        specification.ProviderVersionId);

                    if(!newPublishedProviders.IsNullOrEmpty())
                    {
                        newProviders.AddRange(newPublishedProviders);
                    }
                }

                if (!publishedProviderUpdated)
                {
                    continue;
                }

                if (!newProviders.Contains(publishedProvider))
                {
                    existingPublishedProvidersToUpdate.Add(publishedProvider.Key, publishedProvider.Value);
                }

                publishedProvidersToUpdate.Add(publishedProvider.Key, publishedProvider.Value);
            }
            
            AddInitialPublishVariationReasons(newProviders.Values);

            if(!(await _variationService.ApplyVariations(publishedProvidersToUpdate, newProviders, specification.Id)))
            {
                await _jobManagement.UpdateJobStatus(jobId, 0, 0, false, "Refresh job failed with variations errors.");

                throw new NonRetriableException($"Unable to refresh funding. Variations generated {_variationService.ErrorCount} errors. Check log for details");
            }

            _logger.Information($"Updating a total of {publishedProvidersToUpdate.Count} published providers");

            if (publishedProvidersToUpdate.Count > 0)
            {
                if (existingPublishedProvidersToUpdate.Count > 0 || newProviders.Count > 0)
                {
                    using (Transaction transaction = _transactionFactory.NewTransaction<RefreshService>())
                    {
                        try
                        {
                            // if any error occurs while updating or indexing then we need to re-index all published providers for consistency
                            transaction.Enroll(async () =>
                            {
                                await _publishedProviderVersionService.CreateReIndexJob(author, correlationId);
                            });

                            // Save updated PublishedProviders to cosmos and increment version status
                            if (existingPublishedProvidersToUpdate.Count > 0)
                            {
                                _logger.Information($"Saving updates to existing published providers. Total={existingPublishedProvidersToUpdate.Count}");
                                await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(existingPublishedProvidersToUpdate.Values, author, PublishedProviderStatus.Updated, jobId, correlationId);

                                _logger.Information($"Indexing existing PublishedProviders");
                                await _publishedProviderIndexerService.IndexPublishedProviders(existingPublishedProvidersToUpdate.Values.Select(_ => _.Current));
                            }

                            if (newProviders.Count > 0)
                            {
                                _logger.Information($"Saving new published providers. Total={newProviders.Count}");
                                await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(newProviders.Values, author, PublishedProviderStatus.Draft, jobId, correlationId);

                                _logger.Information($"Indexing newly added PublishedProviders");
                                await _publishedProviderIndexerService.IndexPublishedProviders(newProviders.Values.Select(_ => _.Current));
                            }

                            transaction.Complete();
                        }
                        catch
                        {
                            await transaction.Compensate();

                            throw;
                        }
                    }

                    _logger.Information("Creating generate Csv jobs");

                    IGeneratePublishedFundingCsvJobsCreation generateCsvJobs = _generateCsvJobsLocator
                    .GetService(GeneratePublishingCsvJobsCreationAction.Refresh);
                    IEnumerable<string> fundingLineCodes = await _publishedFundingDataService.GetPublishedProviderFundingLines(specification.Id);
                    IEnumerable<string> fundingStreamIds = Array.Empty<string>();
                    await generateCsvJobs.CreateJobs(specification.Id, correlationId, author, fundingLineCodes, fundingStreamIds, fundingPeriodId);
                }
            }
        }
        
        private void AddInitialPublishVariationReasons(IEnumerable<PublishedProvider> publishedProviders)
        {
            foreach (PublishedProvider publishedProvider in publishedProviders.Where(_ => _.HasResults))
            {
                publishedProvider.Current.AddVariationReasons(VariationReason.FundingUpdated, VariationReason.ProfilingUpdated);
            }
        }
    }
}