using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;

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
        private readonly IPublishedFundingContentsPersistanceService _publishedFundingContentsPersistanceService;
        private readonly IPublishedProviderContentPersistanceService _publishedProviderContentsPersistanceService;
        private readonly IPublishedProviderContentsGeneratorResolver _publishedProviderContentsGeneratorResolver;
        private readonly IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private readonly ISearchRepository<PublishedFundingIndex> _publishedFundingSearchRepository;
        private readonly IGeneratePublishedFundingCsvJobsCreationLocator _generateCsvJobsLocator;
        private readonly AsyncPolicy _calculationsApiClientPolicy;
        private readonly IJobManagement _jobManagement;
        private readonly AsyncPolicy _publishedIndexSearchResiliencePolicy;
        private readonly ITransactionFactory _transactionFactory;
        private readonly IPublishedProviderVersionService _publishedProviderVersionService;
        private readonly IPublishedFundingService _publishedFundingService;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly IPoliciesService _policiesService;
        private readonly ICreatePublishIntegrityJob _createPublishIntegrityJob;

        public PublishService(IPublishedFundingStatusUpdateService publishedFundingStatusUpdateService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            ISpecificationService specificationService,
            IPrerequisiteCheckerLocator prerequisiteCheckerLocator,
            IPublishedFundingChangeDetectorService publishedFundingChangeDetectorService,
            IPublishedFundingGenerator publishedFundingGenerator,
            IPublishedProviderContentsGeneratorResolver publishedProviderContentsGeneratorResolver,
            IPublishedFundingContentsPersistanceService publishedFundingContentsPersistanceService,
            IPublishedProviderContentPersistanceService publishedProviderContentsPersistanceService,
            IPublishedProviderStatusUpdateService publishedProviderStatusUpdateService,
            IProviderService providerService,
            ISearchRepository<PublishedFundingIndex> publishedFundingSearchRepository,
            ICalculationsApiClient calculationsApiClient,
            ILogger logger,
            IJobManagement jobManagement,
            IGeneratePublishedFundingCsvJobsCreationLocator generateCsvJobsLocator,
            ITransactionFactory transactionFactory,
            IPublishedProviderVersionService publishedProviderVersionService,
            IPublishedFundingService publishedFundingService,
            IPublishedFundingDataService publishedFundingDataService,
            IPoliciesService policiesService,
            ICreatePublishIntegrityJob createPublishIntegrityJob) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(generateCsvJobsLocator, nameof(generateCsvJobsLocator));
            Guard.ArgumentNotNull(publishedFundingStatusUpdateService, nameof(publishedFundingStatusUpdateService));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(prerequisiteCheckerLocator, nameof(prerequisiteCheckerLocator));
            Guard.ArgumentNotNull(publishedFundingChangeDetectorService, nameof(publishedFundingChangeDetectorService));
            Guard.ArgumentNotNull(publishedFundingGenerator, nameof(publishedFundingGenerator));
            Guard.ArgumentNotNull(publishedFundingGenerator, nameof(publishedProviderContentsGeneratorResolver));
            Guard.ArgumentNotNull(publishedFundingContentsPersistanceService, nameof(publishedFundingContentsPersistanceService));
            Guard.ArgumentNotNull(publishedProviderContentsPersistanceService, nameof(publishedProviderContentsPersistanceService));
            Guard.ArgumentNotNull(publishedProviderStatusUpdateService, nameof(publishedProviderStatusUpdateService));
            Guard.ArgumentNotNull(publishedFundingSearchRepository, nameof(publishedFundingSearchRepository));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(publishedFundingService, nameof(publishedFundingService));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies.CalculationsApiClient, nameof(publishingResiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedIndexSearchResiliencePolicy, nameof(publishingResiliencePolicies.PublishedIndexSearchResiliencePolicy));
            Guard.ArgumentNotNull(transactionFactory, nameof(transactionFactory));
            Guard.ArgumentNotNull(publishedProviderVersionService, nameof(publishedProviderVersionService));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(createPublishIntegrityJob, nameof(createPublishIntegrityJob));

            _publishedFundingStatusUpdateService = publishedFundingStatusUpdateService;
            _publishedFundingDataService = publishedFundingDataService;
            _specificationService = specificationService;
            _prerequisiteCheckerLocator = prerequisiteCheckerLocator;
            _publishedFundingGenerator = publishedFundingGenerator;
            _publishedProviderContentsGeneratorResolver = publishedProviderContentsGeneratorResolver;
            _publishedFundingContentsPersistanceService = publishedFundingContentsPersistanceService;
            _publishedProviderContentsPersistanceService = publishedProviderContentsPersistanceService;
            _publishedProviderStatusUpdateService = publishedProviderStatusUpdateService;
            _publishedFundingSearchRepository = publishedFundingSearchRepository;
            _logger = logger;
            _calculationsApiClient = calculationsApiClient;
            _calculationsApiClientPolicy = publishingResiliencePolicies.CalculationsApiClient;
            _generateCsvJobsLocator = generateCsvJobsLocator;
            _jobManagement = jobManagement;
            _publishedIndexSearchResiliencePolicy = publishingResiliencePolicies.PublishedIndexSearchResiliencePolicy;
            _transactionFactory = transactionFactory;
            _publishedProviderVersionService = publishedProviderVersionService;
            _providerService = providerService;
            _publishedFundingService = publishedFundingService;
            _policiesService = policiesService;
            _createPublishIntegrityJob = createPublishIntegrityJob;
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
            
            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);

            if (specification == null)
            {
                throw new NonRetriableException($"Could not find specification with id '{specificationId}'");
            }

            string correlationId = message.GetUserProperty<string>(SfaCorrelationId);

            PublishedProviderIdsRequest publishedProviderIdsRequest = null;

            if (batched)
            {
                string publishedProviderIdsRequestJson = message.GetUserProperty<string>(JobConstants.MessagePropertyNames.PublishedProviderIdsRequest);
                publishedProviderIdsRequest = publishedProviderIdsRequestJson.AsPoco<PublishedProviderIdsRequest>();
            }

            foreach (Reference fundingStream in specification.FundingStreams)
            {
                await PublishFundingStream(fundingStream, 
                    specification, 
                    Job.Id, 
                    author, 
                    correlationId,
                    batched ? PrerequisiteCheckerType.ReleaseBatchProviders : PrerequisiteCheckerType.ReleaseAllProviders,
                    publishedProviderIdsRequest?.PublishedProviderIds?.ToArray());
            }

            _logger.Information($"Running search reindexer for published funding");
            await _publishedIndexSearchResiliencePolicy.ExecuteAsync(() => _publishedFundingSearchRepository.RunIndexer());

            await GenerateCsvJobs(specificationId, correlationId, specification, author);
        }

        private async Task GenerateCsvJobs(string specificationId, string correlationId, SpecificationSummary specification, Reference author)
        {
            _logger.Information("Creating generate Csv jobs");

            IGeneratePublishedFundingCsvJobsCreation generateCsvJobs = _generateCsvJobsLocator
                .GetService(GeneratePublishingCsvJobsCreationAction.Release);
            IEnumerable<string> fundingLineCodes = await _publishedFundingDataService.GetPublishedProviderFundingLines(specificationId);
            IEnumerable<string> fundingStreamIds = specification.FundingStreams.Select(fs => fs.Id); //this will only ever be a single I think
            
            PublishedFundingCsvJobsRequest publishedFundingCsvJobsRequest = new PublishedFundingCsvJobsRequest
            {
                SpecificationId = specificationId,
                CorrelationId = correlationId,
                User = author,
                FundingLineCodes = fundingLineCodes,
                FundingStreamIds = fundingStreamIds,
                FundingPeriodId = specification.FundingPeriod.Id,
                IsSpecificationSelectedForFunding = specification.IsSelectedForFunding
            };

            await generateCsvJobs.CreateJobs(publishedFundingCsvJobsRequest);
        }

        private async Task PublishFundingStream(Reference fundingStream,
            SpecificationSummary specification,
            string jobId,
            Reference author,
            string correlationId,
            PrerequisiteCheckerType prerequisiteCheckerType,
            string[] batchPublishedProviderIds = null)
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

            IEnumerable<PublishedProvider> selectedPublishedProviders =
                batchPublishedProviderIds.IsNullOrEmpty() ?
                publishedProvidersForFundingStream.Values :
                batchPublishedProviderIds.Where(_ => publishedProvidersByPublishedProviderId.ContainsKey(_)).Select(_ => publishedProvidersByPublishedProviderId[_]);
            
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
                scopedPublishedProviders?.Values.Select(_ => _.Current.Provider), 
                fundingStream, 
                specification);

            using Transaction transaction = _transactionFactory.NewTransaction<PublishService>();
            try
            {
                // if any error occurs while updating or indexing then we need to re-index all published providers and persist published funding for consistency
                transaction.Enroll(async () =>
                {
                    await _publishedProviderVersionService.CreateReIndexJob(author, correlationId, specification.Id);
                    await _createPublishIntegrityJob.CreateJob(specification.Id, 
                        author, 
                        correlationId,
                        batchPublishedProviderIds.IsNullOrEmpty() ? null : new Dictionary<string, string>
                        {
                            { "providers-batch", JsonExtensions.AsJson(selectedPublishedProviders.Select(_ => _.PublishedProviderId)) }
                        });
                });

                await SavePublishedProvidersAsReleased(jobId, author, selectedPublishedProviders, correlationId);

                ICollection<PublishedProvider> publishedProviders = publishedProvidersForFundingStream?.Values;

                _logger.Information($"Generating published funding");
                IEnumerable<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)> publishedFundingToSave =
                    _publishedFundingGenerator.GeneratePublishedFunding(publishedFundingInput, publishedProviders).ToList();
                _logger.Information($"A total of {publishedFundingToSave.Count()} published funding versions created to save.");

                foreach ((PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion) publishedFundingItems in publishedFundingToSave)
                {
                    PropagateProviderVariationReasons(publishedFundingItems.PublishedFundingVersion, publishedProviders);
                }

                // if any error occurs while updating then we still need to run the indexer to be consistent
                transaction.Enroll(async () =>
                {
                    await _publishedIndexSearchResiliencePolicy.ExecuteAsync(() => _publishedFundingSearchRepository.RunIndexer());
                });

                // Save a version of published funding and set this version to current
                _logger.Information($"Saving published funding");
                await _publishedFundingStatusUpdateService.UpdatePublishedFundingStatus(publishedFundingToSave, author, PublishedFundingStatus.Released, jobId,correlationId);
                _logger.Information($"Finished saving published funding");

                // Save contents to blob storage and search for the feed
                _logger.Information($"Saving published funding contents");
                await _publishedFundingContentsPersistanceService.SavePublishedFundingContents(publishedFundingToSave.Select(_ => _.PublishedFundingVersion),
                    publishedFundingInput.TemplateMetadataContents);
                _logger.Information($"Finished saving published funding contents");

                if (!selectedPublishedProviders.IsNullOrEmpty())
                {
                    // Generate contents JSON for provider and save to blob storage
                    IPublishedProviderContentsGenerator generator = _publishedProviderContentsGeneratorResolver.GetService(publishedFundingInput.TemplateMetadataContents.SchemaVersion);
                    await _publishedProviderContentsPersistanceService.SavePublishedProviderContents(publishedFundingInput.TemplateMetadataContents, templateMapping,
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
                publishedProvider.Current.AddVariationReasons(VariationReason.FundingUpdated, VariationReason.ProfilingUpdated);
            }
        }

        private async Task EnsureJobCanBeProcessed(string jobId)
        {
            try
            {
                await _jobManagement.RetrieveJobAndCheckCanBeProcessed(jobId);
            }
            catch
            {
                string errorMessage = "Job can not be run";
                _logger.Error(errorMessage);

                throw new NonRetriableException(errorMessage);
            }
        }


        private async Task SavePublishedProvidersAsReleased(string jobId, Reference author, IEnumerable<PublishedProvider> publishedProviders,string correlationId)
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