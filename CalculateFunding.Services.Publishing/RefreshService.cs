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
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Publishing
{
    public class RefreshService : JobProcessingService, IRefreshService
    {
        private const string SfaCorrelationId = "sfa-correlationId";

        private readonly IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly ISpecificationService _specificationService;
        private readonly IProviderService _providerService;
        private readonly ICalculationResultsService _calculationResultsService;
        private readonly IPublishedProviderDataGenerator _publishedProviderDataGenerator;
        private readonly IPublishedProviderDataPopulator _publishedProviderDataPopulator;
        private readonly IBatchProfilingService _batchProfilingService;
        private readonly ILogger _logger;
        private readonly ICalculationsApiClient _calculationsApiClient;
        private readonly IPrerequisiteCheckerLocator _prerequisiteCheckerLocator;
        private readonly AsyncPolicy _publishingResiliencePolicy;
        private readonly AsyncPolicy _calculationsApiClientPolicy;
        private readonly IPublishProviderExclusionCheck _providerExclusionCheck;
        private readonly IFundingLineValueOverride _fundingLineValueOverride;
        private readonly IPublishedProviderIndexerService _publishedProviderIndexerService;
        private readonly IJobManagement _jobManagement;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IPublishedProviderVersionService _publishedProviderVersionService;
        private readonly IPoliciesService _policiesService;
        private readonly IVariationService _variationService;
        private readonly IReApplyCustomProfiles _reApplyCustomProfiles;
        private readonly IPublishedProviderErrorDetection _detection;
        private readonly IPublishedFundingCsvJobsService _publishFundingCsvJobsService;

        public RefreshService(IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IPublishedFundingDataService publishedFundingDataService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ISpecificationService specificationService,
            IProviderService providerService,
            ICalculationResultsService calculationResultsService,
            IPublishedProviderDataGenerator publishedProviderDataGenerator,
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
            IReApplyCustomProfiles reApplyCustomProfiles,
            IPublishedProviderErrorDetection detection,
            IBatchProfilingService batchProfilingService,
            IPublishedFundingCsvJobsService publishFundingCsvJobsService) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(calculationResultsService, nameof(calculationResultsService));
            Guard.ArgumentNotNull(publishedProviderDataGenerator, nameof(publishedProviderDataGenerator));
            Guard.ArgumentNotNull(publishedProviderDataPopulator, nameof(publishedProviderDataPopulator));
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
            Guard.ArgumentNotNull(detection, nameof(detection));
            Guard.ArgumentNotNull(publishingResiliencePolicies.CalculationsApiClient, nameof(publishingResiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(batchProfilingService, nameof(batchProfilingService));
            Guard.ArgumentNotNull(publishFundingCsvJobsService, nameof(publishFundingCsvJobsService));

            _publishedProviderStatusUpdateService = publishedProviderStatusUpdateService;
            _publishedFundingDataService = publishedFundingDataService;
            _specificationService = specificationService;
            _providerService = providerService;
            _calculationResultsService = calculationResultsService;
            _publishedProviderDataGenerator = publishedProviderDataGenerator;
            _publishedProviderDataPopulator = publishedProviderDataPopulator;
            _logger = logger;
            _calculationsApiClient = calculationsApiClient;
            _prerequisiteCheckerLocator = prerequisiteCheckerLocator;
            _providerExclusionCheck = providerExclusionCheck;
            _fundingLineValueOverride = fundingLineValueOverride;
            _variationService = variationService;
            _reApplyCustomProfiles = reApplyCustomProfiles;
            _detection = detection;
            _batchProfilingService = batchProfilingService;
            _publishedProviderIndexerService = publishedProviderIndexerService;

            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _calculationsApiClientPolicy = publishingResiliencePolicies.CalculationsApiClient;
            _jobManagement = jobManagement;
            _transactionFactory = transactionFactory;
            _publishedProviderVersionService = publishedProviderVersionService;
            _policiesService = policiesService;
            _publishFundingCsvJobsService = publishFundingCsvJobsService;
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            Reference author = message.GetUserDetails();

            string specificationId = message.UserProperties["specification-id"] as string;

            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);

            if (specification == null)
            {
                throw new NonRetriableException($"Could not find specification with id '{specificationId}'");
            }

            // Get scoped providers for this specification
            IDictionary<string, Provider> scopedProviders = await _providerService.GetScopedProvidersForSpecification(specification.Id, specification.ProviderVersionId);

            if (!scopedProviders.IsNullOrEmpty())
            {
                _logger.Information($"Found {scopedProviders.Count} scoped providers for refresh");
            }
            else
            {
                _logger.Information("No scoped providers found for refresh");
            }

            // Get existing published providers for this specification
            _logger.Information("Looking up existing published providers from cosmos for refresh job");

            IDictionary<string, List<PublishedProvider>> existingPublishedProvidersByFundingStream = new Dictionary<string, List<PublishedProvider>>();
            foreach (Reference fundingStream in specification.FundingStreams)
            {
                List<PublishedProvider> publishedProviders = (await _publishingResiliencePolicy.ExecuteAsync(() =>
                _publishedFundingDataService.GetCurrentPublishedProviders(fundingStream.Id, specification.FundingPeriod.Id))).ToList();

                existingPublishedProvidersByFundingStream.Add(fundingStream.Id, publishedProviders);
                _logger.Information($"Found {publishedProviders.Count} existing published providers for funding stream {fundingStream.Id} from cosmos for refresh job");
            }

            _logger.Information("Verifying prerequisites for funding refresh");

            // Check prerequisites for this specification to be chosen/refreshed
            IPrerequisiteChecker prerequisiteChecker = _prerequisiteCheckerLocator.GetPreReqChecker(PrerequisiteCheckerType.Refresh);
            try
            {
                await prerequisiteChecker.PerformChecks(specification, Job.Id, existingPublishedProvidersByFundingStream.SelectMany(x => x.Value), scopedProviders?.Values);
            }
            catch (JobPrereqFailedException ex)
            {
                throw new NonRetriableException(ex.Message, ex);
            }
            _logger.Information("Prerequisites for refresh passed");

            // Get calculation results for specification 
            _logger.Information("Looking up calculation results");

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
                    _logger.Information($"Starting to refresh funding for '{fundingStream.Id}'");

                    await RefreshFundingStream(fundingStream,
                        specification,
                        scopedProviders,
                        allCalculationResults,
                        Job.Id,
                        author,
                        correlationId,
                        existingPublishedProvidersByFundingStream[fundingStream.Id]);

                    _logger.Information($"Finished processing refresh funding for '{fundingStream.Id}'");

                }
            }
            finally
            {
                _logger.Information("Starting to clear variation snapshots");
                _variationService.ClearSnapshots();
                _logger.Information("Finished clearing variation snapshots");
            }
        }

        private async Task RefreshFundingStream(Reference fundingStream,
            SpecificationSummary specification,
            IDictionary<string, Provider> scopedProviders,
            IDictionary<string, ProviderCalculationResult> allCalculationResults,
            string jobId, Reference author,
            string correlationId,
            IEnumerable<PublishedProvider> existingPublishedProviders)
        {
            TemplateMetadataContents templateMetadataContents = await _policiesService.GetTemplateMetadataContents(fundingStream.Id, specification.FundingPeriod.Id, specification.TemplateIds[fundingStream.Id]);

            if (templateMetadataContents == null)
            {
                _logger.Information($"Unable to locate template meta data contents for funding stream:'{fundingStream.Id}' and template id:'{specification.TemplateIds[fundingStream.Id]}'");
                return;
            }

            IEnumerable<ProfileVariationPointer> variationPointers = await _specificationService.GetProfileVariationPointers(specification.Id) ?? ArraySegment<ProfileVariationPointer>.Empty;

            Dictionary<string, PublishedProvider> publishedProviders = new Dictionary<string, PublishedProvider>();

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

            Dictionary<string, PublishedProvider> existingPublishedProvidersToRemove = new Dictionary<string, PublishedProvider>();

            FundingLine[] flattenedTemplateFundingLines = templateMetadataContents.RootFundingLines.Flatten(_ => _.FundingLines).ToArray();

            _logger.Information("Profiling providers for refresh");

            try
            {
                await ProfileProviders(publishedProviders, newProviders, generatedPublishedProviderData);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Exception during generating provider profiling");

                if (ex is NonRetriableException)
                {
                    await _jobManagement.UpdateJobStatus(jobId, 0, 0, false, "Refresh job failed during generating provider profiling.");
                }

                throw;
            }

            _logger.Information("Finished profiling providers for refresh");

            _logger.Information("Start snapshots for published provider variations");
            // snapshot the current published providers so any changes aren't reflected when we detect variations later
            _variationService.SnapShot(publishedProviders, fundingStream.Id);
            _logger.Information("Finished snapshots for published provider variations");

            //we need enumerate a readonly cut of this as we add to it in some variations now (for missing providers not in scope)
            Dictionary<string, PublishedProvider> publishedProvidersReadonlyDictionary = publishedProviders.ToDictionary(_ => _.Key, _ => _.Value);

            _logger.Information($"Start getting funding configuration for funding stream '{fundingStream.Id}'");
            // set up the published providers context for error detection laterawait 
            FundingConfiguration fundingConfiguration = await _policiesService.GetFundingConfiguration(fundingStream.Id, specification.FundingPeriod.Id);
            _logger.Information($"Retrieved funding stream configuration for '{fundingStream.Id}'");

            PublishedProvidersContext publishedProvidersContext = new PublishedProvidersContext
            {
                ScopedProviders = scopedProviders.Values,
                SpecificationId = specification.Id,
                ProviderVersionId = specification.ProviderVersionId,
                CurrentPublishedFunding = (await _publishingResiliencePolicy.ExecuteAsync(() => _publishedFundingDataService.GetCurrentPublishedFunding(specification.Id, GroupingReason.Payment)))
                    .Where(x => x.Current.GroupingReason == CalculateFunding.Models.Publishing.GroupingReason.Payment),
                OrganisationGroupResultsData = new Dictionary<string, IEnumerable<OrganisationGroupResult>>(),
                FundingConfiguration = fundingConfiguration
            };

            _logger.Information("Starting to process providers for variations and exclusions");

            foreach (KeyValuePair<string, PublishedProvider> publishedProvider in publishedProvidersReadonlyDictionary)
            {
                PublishedProviderVersion publishedProviderVersion = publishedProvider.Value.Current;

                // need to reset the variation reasons so we don't carry over variation reasons on a refresh
                publishedProviderVersion.VariationReasons = Array.Empty<VariationReason>();

                string providerId = publishedProviderVersion.ProviderId;

                bool providerExists = scopedProviders.ContainsKey(providerId);

                // Handle the case where a provider has a record in funding approvals
                // but when refresh funding is run, it's now no longer in the specification's scoped provider list
                if (!providerExists)
                {
                    publishedProvidersToUpdate.Add(publishedProvider.Key, publishedProvider.Value);

                    // When there is no released funding for this provider
                    if (publishedProvider.Value.Released == null)
                    {
                        existingPublishedProvidersToRemove.Add(publishedProvider.Key, publishedProvider.Value);
                        continue;
                    }
                    else
                    {
                        existingPublishedProvidersToUpdate.Add(publishedProvider.Key, publishedProvider.Value);
                    }
                }

                generatedPublishedProviderData.TryGetValue(publishedProvider.Key, out GeneratedProviderResult generatedProviderResult);

                PublishedProviderExclusionCheckResult exclusionCheckResult =
                    _providerExclusionCheck.ShouldBeExcluded(publishedProvider.Key, generatedProviderResult, flattenedTemplateFundingLines);
                
                bool publishedProviderUpdated = false;

                if (providerExists)
                {
                    if (exclusionCheckResult.ShouldBeExcluded)
                    {
                        if (newProviders.ContainsKey(publishedProvider.Key))
                        {
                            newProviders.Remove(publishedProvider.Key);
                            continue;
                        }

                        // if there is no previous funding for the generated funding lines then remove
                        if (publishedProvider.Value.Released == null || !_fundingLineValueOverride.HasPreviousFunding(generatedProviderResult, publishedProviderVersion))
                        {
                            existingPublishedProvidersToRemove.Add(publishedProvider.Key, publishedProvider.Value);
                            publishedProvidersToUpdate.Add(publishedProvider.Key, publishedProvider.Value);
                            continue;
                        }
                    }

                    _fundingLineValueOverride.OverridePreviousFundingLineValues(publishedProvider.Value, generatedProviderResult);

                    publishedProviderUpdated = _publishedProviderDataPopulator.UpdatePublishedProvider(publishedProviderVersion,
                        generatedProviderResult,
                        scopedProviders[providerId],
                        specification.TemplateIds[fundingStream.Id],
                        newProviders.ContainsKey(publishedProvider.Key));

                    _logger.Verbose($"Published provider '{publishedProvider.Key}' updated: '{publishedProviderUpdated}'");

                    //reapply any custom profiles this provider has and internally check for errors
                    _reApplyCustomProfiles.ProcessPublishedProvider(publishedProviderVersion);
                }

                // process published provider and detect errors
                if (await _detection.ApplyRefreshPreVariationErrorDetection(publishedProvider.Value, publishedProvidersContext))
                {
                    publishedProviderUpdated = true;
                }

                if (publishedProviderUpdated && existingPublishedProviders.AnyWithNullCheck() && scopedProviders.ContainsKey(providerId))
                {
                    IDictionary<string, PublishedProvider> newPublishedProviders = await _variationService.PrepareVariedProviders(generatedProviderResult.TotalFunding ?? 0,
                        publishedProviders,
                        publishedProvider.Value,
                        scopedProviders[providerId],
                        fundingConfiguration?.Variations,
                        variationPointers,
                        fundingStream.Id,
                        specification.ProviderVersionId);

                    if (!newPublishedProviders.IsNullOrEmpty())
                    {
                        newProviders.AddRange(newPublishedProviders);
                    }
                }

                if (!publishedProviderUpdated)
                {
                    continue;
                }

                if (!newProviders.ContainsKey(publishedProvider.Key))
                {
                    existingPublishedProvidersToUpdate.TryAdd(publishedProvider.Key, publishedProvider.Value);
                }

                publishedProvidersToUpdate.TryAdd(publishedProvider.Key, publishedProvider.Value);
            }

            _logger.Information("Finished processing providers for variations and exclusions");

            _logger.Information("Adding additional variation reasons");
            AddInitialPublishVariationReasons(newProviders.Values);
            _logger.Information("Finished adding additional variation reasons");

            _logger.Information("Starting to apply variations");

            if (!await _variationService.ApplyVariations(publishedProvidersToUpdate, newProviders, specification.Id, jobId))
            {
                await _jobManagement.UpdateJobStatus(jobId, 0, 0, false, "Refresh job failed with variations errors.");

                throw new NonRetriableException($"Unable to refresh funding. Variations generated {_variationService.ErrorCount} errors. Check log for details");
            }

            _logger.Information("Finished applying variations");

            _logger.Information($"Updating a total of {publishedProvidersToUpdate.Count} published providers");

            //apply any post variation error detection that we also need to run
            foreach (PublishedProvider publishedProvider in publishedProvidersToUpdate.Values)
            {
                await _detection.ApplyRefreshPostVariationsErrorDetection(publishedProvider, publishedProvidersContext);
            }

            if (publishedProvidersToUpdate.Count > 0)
            {
                if (existingPublishedProvidersToUpdate.Count > 0 || newProviders.Count > 0 || existingPublishedProvidersToRemove.Count > 0)
                {
                    using (Transaction transaction = _transactionFactory.NewTransaction<RefreshService>())
                    {
                        try
                        {
                            // if any error occurs while updating or indexing then we need to re-index all published providers for consistency
                            transaction.Enroll(async () =>
                            {
                                await _publishedProviderVersionService.CreateReIndexJob(author, correlationId, specification.Id, jobId);
                            });

                            // Save updated PublishedProviders to cosmos and increment version status
                            if (existingPublishedProvidersToUpdate.Count > 0)
                            {
                                _logger.Information($"Saving updates to existing published providers. Total={existingPublishedProvidersToUpdate.Count}");

                                IEnumerable<PublishedProvider> draftPublishedProviders = existingPublishedProvidersToUpdate.Values.Where(_ => _.Current.Status == PublishedProviderStatus.Draft);
                                IEnumerable<PublishedProvider> nonDraftPublishedProviders = existingPublishedProvidersToUpdate.Values.Except(draftPublishedProviders);

                                await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(nonDraftPublishedProviders, author, PublishedProviderStatus.Updated, jobId, correlationId);
                                await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(draftPublishedProviders, author, PublishedProviderStatus.Draft, jobId, correlationId);

                                _logger.Information("Indexing existing PublishedProviders");
                                await _publishedProviderIndexerService.IndexPublishedProviders(existingPublishedProvidersToUpdate.Values.Select(_ => _.Current));
                            }

                            if (newProviders.Count > 0)
                            {
                                _logger.Information($"Saving new published providers. Total={newProviders.Count}");
                                await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(newProviders.Values, author, PublishedProviderStatus.Draft, jobId, correlationId);

                                _logger.Information("Indexing newly added PublishedProviders");
                                await _publishedProviderIndexerService.IndexPublishedProviders(newProviders.Values.Select(_ => _.Current));
                            }

                            if (existingPublishedProvidersToRemove.Count > 0)
                            {
                                _logger.Information($"Deleting existing published providers. Total={existingPublishedProvidersToRemove.Count}");
                                await _publishedFundingDataService.DeletePublishedProviders(existingPublishedProvidersToRemove.Values);

                                _logger.Information("Removing index of existing PublishedProviders");
                                await _publishedProviderIndexerService.Remove(existingPublishedProvidersToRemove.Values.Select(_ => _.Current));
                            }

                            transaction.Complete();
                        }
                        catch (Exception ex)
                        {
                            await transaction.Compensate();

                            throw;
                        }
                    }

                    _logger.Information("Creating generate Csv jobs");

                    await _publishFundingCsvJobsService.GenerateCsvJobs(GeneratePublishingCsvJobsCreationAction.Refresh, specification.Id, specification.FundingPeriod.Id, specification.FundingStreams.Select(_ => _.Id) , correlationId, author);
                }
            }
        }

        private async Task ProfileProviders(IDictionary<string, PublishedProvider> publishedProviders,
        IDictionary<string, PublishedProvider> newProviders,
        IDictionary<string, GeneratedProviderResult> generatedPublishedProviderData)
        {
            BatchProfilingContext batchProfilingContext = new BatchProfilingContext();

            foreach (KeyValuePair<string, PublishedProvider> publishedProvider in publishedProviders)
            {
                bool isNewProvider = newProviders.ContainsKey(publishedProvider.Key);

                PublishedProviderVersion publishedProviderVersion = publishedProvider.Value.Current;

                if (!generatedPublishedProviderData.ContainsKey(publishedProviderVersion.ProviderId))
                {
                    continue;
                }

                batchProfilingContext.AddProviderProfilingRequestData(publishedProviderVersion,
                    generatedPublishedProviderData,
                    isNewProvider);
            }

            await _batchProfilingService.ProfileBatches(batchProfilingContext);
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