using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class PublishService : JobProcessingService, IPublishService
    {
        private const string SfaCorrelationId = "sfa-correlationId";

        private readonly IPublishedFundingStatusUpdateService _publishedFundingStatusUpdateService;
        private readonly ISpecificationService _specificationService;

        private readonly IProviderService _providerService;
        private readonly ILogger _logger;
        private readonly ICalculationsApiClient _calculationsApiClient;
        private readonly IPrerequisiteCheckerLocator _prerequisiteCheckerLocator;
        private readonly IPublishedFundingGenerator _publishedFundingGenerator;
        private readonly IPublishedFundingContentsPersistenceService _publishedFundingContentsPersistenceService;
        private readonly IPublishedProviderContentPersistenceService _publishedProviderContentsPersistenceService;
        private readonly IPublishedProviderContentsGeneratorResolver _publishedProviderContentsGeneratorResolver;
        private readonly IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private readonly ISearchRepository<PublishedFundingIndex> _publishedFundingSearchRepository;
        private readonly AsyncPolicy _calculationsApiClientPolicy;
        private readonly AsyncPolicy _publishedIndexSearchResiliencePolicy;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IPublishedProviderVersionService _publishedProviderVersionService;
        private readonly IPublishedFundingService _publishedFundingService;
        private readonly ICreatePublishIntegrityJob _createPublishIntegrityJob;
        private readonly IPostReleaseJobCreationService _postReleaseJobCreationService;

        public PublishService(IPublishedFundingStatusUpdateService publishedFundingStatusUpdateService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ISpecificationService specificationService,
            IPrerequisiteCheckerLocator prerequisiteCheckerLocator,
            IPublishedFundingChangeDetectorService publishedFundingChangeDetectorService,
            IPublishedFundingGenerator publishedFundingGenerator,
            IPublishedProviderContentsGeneratorResolver publishedProviderContentsGeneratorResolver,
            IPublishedFundingContentsPersistenceService publishedFundingContentsPersistenceService,
            IPublishedProviderContentPersistenceService publishedProviderContentsPersistenceService,
            IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IProviderService providerService,
            ISearchRepository<PublishedFundingIndex> publishedFundingSearchRepository,
            ICalculationsApiClient calculationsApiClient,
            ILogger logger,
            IJobManagement jobManagement,
            ITransactionFactory transactionFactory,
            IPublishedProviderVersionService publishedProviderVersionService,
            IPublishedFundingService publishedFundingService,
            ICreatePublishIntegrityJob createPublishIntegrityJob,
            IPostReleaseJobCreationService postReleaseJobCreationService) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(publishedFundingStatusUpdateService, nameof(publishedFundingStatusUpdateService));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(prerequisiteCheckerLocator, nameof(prerequisiteCheckerLocator));
            Guard.ArgumentNotNull(publishedFundingChangeDetectorService, nameof(publishedFundingChangeDetectorService));
            Guard.ArgumentNotNull(publishedFundingGenerator, nameof(publishedFundingGenerator));
            Guard.ArgumentNotNull(publishedFundingGenerator, nameof(publishedProviderContentsGeneratorResolver));
            Guard.ArgumentNotNull(publishedFundingContentsPersistenceService, nameof(publishedFundingContentsPersistenceService));
            Guard.ArgumentNotNull(publishedProviderContentsPersistenceService, nameof(publishedProviderContentsPersistenceService));
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingSearchRepository, nameof(publishedFundingSearchRepository));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(publishedFundingService, nameof(publishedFundingService));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies.CalculationsApiClient, nameof(publishingResiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedIndexSearchResiliencePolicy, nameof(publishingResiliencePolicies.PublishedIndexSearchResiliencePolicy));
            Guard.ArgumentNotNull(transactionFactory, nameof(transactionFactory));
            Guard.ArgumentNotNull(publishedProviderVersionService, nameof(publishedProviderVersionService));
            Guard.ArgumentNotNull(createPublishIntegrityJob, nameof(createPublishIntegrityJob));
            Guard.ArgumentNotNull(postReleaseJobCreationService, nameof(postReleaseJobCreationService));

            _publishedFundingStatusUpdateService = publishedFundingStatusUpdateService;
            _specificationService = specificationService;
            _prerequisiteCheckerLocator = prerequisiteCheckerLocator;
            _publishedFundingGenerator = publishedFundingGenerator;
            _publishedProviderContentsGeneratorResolver = publishedProviderContentsGeneratorResolver;
            _publishedFundingContentsPersistenceService = publishedFundingContentsPersistenceService;
            _publishedProviderContentsPersistenceService = publishedProviderContentsPersistenceService;
            _publishedProviderStatusUpdateService = publishedProviderStatusUpdateService;
            _publishedFundingSearchRepository = publishedFundingSearchRepository;
            _logger = logger;
            _calculationsApiClient = calculationsApiClient;
            _calculationsApiClientPolicy = publishingResiliencePolicies.CalculationsApiClient;
            _publishedIndexSearchResiliencePolicy = publishingResiliencePolicies.PublishedIndexSearchResiliencePolicy;
            _transactionFactory = transactionFactory;
            _publishedProviderVersionService = publishedProviderVersionService;
            _providerService = providerService;
            _publishedFundingService = publishedFundingService;
            _createPublishIntegrityJob = createPublishIntegrityJob;
            _postReleaseJobCreationService = postReleaseJobCreationService;
        }

        public override async Task Process(Message message)
        {
            await PublishProviderFundingResults(message);
        }

        public async Task PublishProviderFundingResults(Message message, bool batched = false)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string logMessage = batched ? "Batch" : "All";
            _logger.Information($"Starting Publish{logMessage}ProviderFundingResults job");

            Reference author = message.GetUserDetails();

            string specificationId = message.UserProperties["specification-id"] as string;

            string correlationId = message.GetUserProperty<string>(SfaCorrelationId);

            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);

            if (specification == null)
            {
                throw new NonRetriableException($"Could not find specification with id '{specificationId}'");
            }

            PublishedProviderIdsRequest publishedProviderIdsRequest = null;

            if (batched)
            {
                publishedProviderIdsRequest = message.GetPayloadAsInstanceOf<PublishedProviderIdsRequest>();
            }

            await PublishProviderFundingResults(batched,
                                                author,
                                                Job.Id,
                                                correlationId,
                                                specification,
                                                publishedProviderIdsRequest,
                                                true);
        }

        public async Task PublishProviderFundingResults(bool batched,
                                                        Reference author,
                                                        string jobId,
                                                        string correlationId,
                                                        SpecificationSummary specification,
                                                        PublishedProviderIdsRequest publishedProviderIdsRequest,
                                                        bool publishFundingJobWithv3Only)
        {
            Guard.ArgumentNotNull(author, nameof(author));
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));
            Guard.IsNullOrWhiteSpace(correlationId, nameof(correlationId));
            Guard.ArgumentNotNull(specification, nameof(specification));

            if (batched)
            {
                Guard.ArgumentNotNull(publishedProviderIdsRequest, nameof(publishedProviderIdsRequest));
            }

            foreach (Reference fundingStream in specification.FundingStreams)
            {
                await PublishFundingStream(fundingStream,
                    specification,
                    jobId,
                    author,
                    correlationId,
                    batched ? PrerequisiteCheckerType.ReleaseBatchProviders : PrerequisiteCheckerType.ReleaseAllProviders,
                    publishedProviderIdsRequest?.PublishedProviderIds?.ToArray(),
                    publishFundingJobWithv3Only);
            }

            _logger.Information($"Running search reindexer for published funding");
            await _publishedIndexSearchResiliencePolicy.ExecuteAsync(() => _publishedFundingSearchRepository.RunIndexer());

            if (publishFundingJobWithv3Only)
            {
                await _postReleaseJobCreationService.QueueJobs(specification, correlationId, author);
            }
        }

        private async Task PublishFundingStream(Reference fundingStream,
            SpecificationSummary specification,
            string jobId,
            Reference author,
            string correlationId,
            PrerequisiteCheckerType prerequisiteCheckerType,
            string[] batchPublishedProviderIds = null,
            bool publishFundingJobWithv3Only = true)
        {
            _logger.Information($"Processing Publish Funding for {fundingStream.Id} in specification {specification.Id}");

            if (!specification.TemplateIds.ContainsKey(fundingStream.Id) || string.IsNullOrWhiteSpace(specification.TemplateIds[fundingStream.Id]))
            {
                _logger.Information($"Skipped publishing {fundingStream.Id} as no template exists");

                return;
            }

            // we always need to get every provider in scope whether it is released or otherwise so that we always genarate the contents
            // this is just in case an error has occurred during a release so we never get a case where we don't get blobs generated for the published providers
            (IDictionary<string, PublishedProvider> publishedProvidersForFundingStream,
                IDictionary<string, PublishedProvider> scopedPublishedProviders) = await _providerService.GetPublishedProviders(fundingStream,
                        specification);

            IDictionary<string, PublishedProvider> publishedProvidersByPublishedProviderId = publishedProvidersForFundingStream.Values.ToDictionary(_ => _.PublishedProviderId);

            PublishedProvider[] selectedPublishedProviders =
                 batchPublishedProviderIds.IsNullOrEmpty() ?
                 publishedProvidersForFundingStream.Values.ToArray() :
                 batchPublishedProviderIds.Where(_ => publishedProvidersByPublishedProviderId.ContainsKey(_)).Select(_ => publishedProvidersByPublishedProviderId[_]).ToArray();

            _logger.Information($"A total of '{selectedPublishedProviders.Length}' selected published providers were found to publish");

            Dictionary<string, PublishedProvider> endStateReleasedProviders = publishedProvidersForFundingStream.Values.Where(_ => _.Released != null).ToDictionary(_ => _.Current.ProviderId);

            foreach (PublishedProvider publishedProvider in selectedPublishedProviders)
            {
                if (!endStateReleasedProviders.ContainsKey(publishedProvider.Current.ProviderId))
                {
                    if (!publishedProvidersForFundingStream.ContainsKey(publishedProvider.Current.ProviderId))
                    {
                        throw new InvalidOperationException($"Unable to find published provider with key '{publishedProvider.Current.ProviderId}'");
                    }

                    endStateReleasedProviders.Add(publishedProvider.Current.ProviderId, publishedProvider);
                }
            }

            _logger.Information("Added initial variation reasons");

            AddInitialPublishVariationReasons(selectedPublishedProviders);

            _logger.Information($"Verifying prerequisites for funding publish");

            IPrerequisiteChecker prerequisiteChecker = _prerequisiteCheckerLocator.GetPreReqChecker(prerequisiteCheckerType);

            try
            {
                await prerequisiteChecker.PerformChecks(specification, jobId, selectedPublishedProviders?.ToList());
            }
            catch (JobPrereqFailedException ex)
            {
                throw new NonRetriableException(ex.Message, ex);
            }

            _logger.Information("Prerequisites for publish passed");

            TemplateMapping templateMapping = await GetTemplateMapping(fundingStream, specification.Id);

            PublishedFundingInput publishedFundingInput = await _publishedFundingService.GeneratePublishedFundingInput(publishedProvidersForFundingStream,
                endStateReleasedProviders?.Values.Select(_ => _.Current.Provider),
                fundingStream,
                specification,
                batchPublishedProviderIds.IsNullOrEmpty() ? null : selectedPublishedProviders);

            using Transaction transaction = _transactionFactory.NewTransaction<PublishService>();
            try
            {
                // if any error occurs while updating or indexing then we need to re-index all published providers and persist published funding for consistency
                transaction.Enroll(async () =>
                {
                    if (publishFundingJobWithv3Only)
                    {
                        await _publishedProviderVersionService.CreateReIndexJob(author, correlationId, specification.Id, jobId);
                        await _createPublishIntegrityJob.CreateJob(specification.Id,
                            author,
                            correlationId,
                            batchPublishedProviderIds.IsNullOrEmpty() ? null : new Dictionary<string, string>
                            {
                            { "providers-batch", JsonExtensions.AsJson(selectedPublishedProviders.Select(_ => _.PublishedProviderId)) }
                            },
                            parentJobId: jobId);
                    }
                });

                await SavePublishedProvidersAsReleased(jobId, author, selectedPublishedProviders, correlationId);

                ICollection<PublishedProvider> publishedProviders = publishedProvidersForFundingStream?.Values;

                _logger.Information($"Generating published funding");
                IEnumerable<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)> publishedFundingToSave =
                    _publishedFundingGenerator.GeneratePublishedFunding(
                        publishedFundingInput,
                        publishedProviders,
                        author,
                        jobId,
                        correlationId)
                    .ToList();
                _logger.Information($"A total of {publishedFundingToSave.Count()} published funding versions created to save.");

                foreach ((PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion) publishedFundingItems in publishedFundingToSave)
                {
                    PropagateProviderVariationReasons(publishedFundingItems.PublishedFundingVersion, publishedProviders);
                }

                // if any error occurs while updating then we still need to run the indexer to be consistent
                transaction.Enroll(async () =>
                {
                    if (publishFundingJobWithv3Only)
                    {
                        await _publishedIndexSearchResiliencePolicy.ExecuteAsync(() => _publishedFundingSearchRepository.RunIndexer());
                    }
                });

                // Save a version of published funding and set this version to current
                _logger.Information($"Saving published funding");
                await _publishedFundingStatusUpdateService.UpdatePublishedFundingStatus(publishedFundingToSave, PublishedFundingStatus.Released);
                _logger.Information($"Finished saving published funding");

                // Save contents to blob storage and search for the feed
                _logger.Information($"Saving published funding contents");
                await _publishedFundingContentsPersistenceService.SavePublishedFundingContents(publishedFundingToSave.Select(_ => _.PublishedFundingVersion),
                    publishedFundingInput.TemplateMetadataContents);
                _logger.Information($"Finished saving published funding contents");

                if (!selectedPublishedProviders.IsNullOrEmpty())
                {
                    // Generate contents JSON for provider and save to blob storage
                    IPublishedProviderContentsGenerator generator = _publishedProviderContentsGeneratorResolver.GetService(publishedFundingInput.TemplateMetadataContents.SchemaVersion);
                    await _publishedProviderContentsPersistenceService.SavePublishedProviderContents(publishedFundingInput.TemplateMetadataContents, templateMapping,
                        selectedPublishedProviders, generator);
                }

                transaction.Complete();
            }
            catch (Exception ex)
            {
                await transaction.Compensate();

                throw;
            }
        }

        private void PropagateProviderVariationReasons(PublishedFundingVersion publishedFundingVersion, IEnumerable<PublishedProvider> publishedProviders)
        {
            foreach (PublishedProvider publishedProvider in publishedProviders)
            {
                publishedFundingVersion.AddVariationReasons(publishedProvider.Current.VariationReasons);
            }
        }

        private void AddInitialPublishVariationReasons(IEnumerable<PublishedProvider> publishedProviders)
        {
            foreach (PublishedProvider publishedProvider in publishedProviders)
            {
                PublishedProviderVersion current = publishedProvider.Current;

                if (publishedProvider.Released == null || current.MajorVersion == 1 && current.MinorVersion == 0)
                {
                    current.AddVariationReasons(VariationReason.FundingUpdated, VariationReason.ProfilingUpdated);
                }
            }
        }

        private async Task SavePublishedProvidersAsReleased(string jobId, Reference author, IEnumerable<PublishedProvider> publishedProviders, string correlationId)
        {
            IEnumerable<PublishedProvider> publishedProvidersToSaveAsReleased = publishedProviders.Where(p => p.Current.Status != PublishedProviderStatus.Released);

            _logger.Information($"Saving published providers. Total = '{publishedProvidersToSaveAsReleased.Count()}'");

            await _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(publishedProvidersToSaveAsReleased, author, PublishedProviderStatus.Released,
                jobId, correlationId);

            _logger.Information($"Finished saving published funding contents");
        }

        private async Task<TemplateMapping> GetTemplateMapping(Reference fundingStream, string specificationId)
        {
            ApiResponse<TemplateMapping> calculationMappingResult =
                await _calculationsApiClientPolicy.ExecuteAsync(() => _calculationsApiClient.GetTemplateMapping(specificationId, fundingStream.Id));

            if (calculationMappingResult == null)
            {
                throw new Exception($"calculationMappingResult returned null for funding stream {fundingStream.Id}");
            }

            return calculationMappingResult.Content;
        }
    }
}