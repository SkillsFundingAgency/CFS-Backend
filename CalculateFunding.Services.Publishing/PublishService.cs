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
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishService : IPublishService
    {
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
            IPublishedFundingDataService publishedFundingDataService)
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
        }

        public async Task PublishBatchProviderFundingResults(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            _logger.Information("Starting PublishBatchProviderFundingResults job");

            Reference author = message.GetUserDetails();

            string specificationId = message.UserProperties["specification-id"] as string;
            string jobId = message.UserProperties["jobId"]?.ToString();

            await EnsureJobCanBeProcessed(jobId);

            // Update job to set status to processing
            await _jobManagement.UpdateJobStatus(jobId, 0, 0, null, null);

            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);

            if (specification == null)
            {
                throw new NonRetriableException($"Could not find specification with id '{specificationId}'");
            }

            string correlationId = message.GetUserProperty<string>("correlation-id");

            string publishProvidersRequestJson = message.GetUserProperty<string>(JobConstants.MessagePropertyNames.PublishProvidersRequest);
            PublishProvidersRequest publishProvidersRequest = JsonExtensions.AsPoco<PublishProvidersRequest>(publishProvidersRequestJson);

            foreach (Reference fundingStream in specification.FundingStreams)
            {
                await PublishFundingStream(fundingStream, specification, jobId, author, correlationId, 
                    PrerequisiteCheckerType.ReleaseBatchProviders, publishProvidersRequest?.Providers?.ToArray());
            }

            _logger.Information($"Running search reindexer for published funding");
            await _publishedIndexSearchResiliencePolicy.ExecuteAsync(() => _publishedFundingSearchRepository.RunIndexer());

            await GenerateCsvJobs(specificationId, correlationId, specification, author);

            // Mark job as complete
            _logger.Information($"Marking publish funding job complete");

            await _jobManagement.UpdateJobStatus(jobId, 0, 0, true, null);
            _logger.Information($"Publish funding job complete");
        }

        public async Task PublishAllProviderFundingResults(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            _logger.Information("Starting PublishAllProviderFundingResults job");

            Reference author = message.GetUserDetails();

            string specificationId = message.UserProperties["specification-id"] as string;
            string jobId = message.UserProperties["jobId"]?.ToString();

            await EnsureJobCanBeProcessed(jobId);

            // Update job to set status to processing
            await _jobManagement.UpdateJobStatus(jobId, 0, 0, null, null);

            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);

            if (specification == null)
            {
                throw new NonRetriableException($"Could not find specification with id '{specificationId}'");
            }

            string correlationId = message.GetUserProperty<string>("correlation-id");

            foreach (Reference fundingStream in specification.FundingStreams)
            {
                await PublishFundingStream(fundingStream, specification, jobId, author, correlationId, PrerequisiteCheckerType.ReleaseAllProviders);
            }

            _logger.Information($"Running search reindexer for published funding");
            await _publishedIndexSearchResiliencePolicy.ExecuteAsync(() => _publishedFundingSearchRepository.RunIndexer());

            await GenerateCsvJobs(specificationId, correlationId, specification, author);

            // Mark job as complete
            _logger.Information($"Marking publish funding job complete");

            await _jobManagement.UpdateJobStatus(jobId, 0, 0, true, null);
            _logger.Information($"Publish funding job complete");
        }

        private async Task GenerateCsvJobs(string specificationId, string correlationId, SpecificationSummary specification, Reference author)
        {
            _logger.Information("Creating generate Csv jobs");

            IGeneratePublishedFundingCsvJobsCreation generateCsvJobs = _generateCsvJobsLocator
                .GetService(GeneratePublishingCsvJobsCreationAction.Release);
            IEnumerable<string> fundingLineCodes = await _publishedFundingDataService.GetPublishedProviderFundingLines(specificationId);
            IEnumerable<string> fundingStreamIds = specification.FundingStreams.Select(fs => fs.Id); //this will only ever be a single I think
            await generateCsvJobs.CreateJobs(specificationId, correlationId, author, fundingLineCodes, fundingStreamIds, specification.FundingPeriod?.Id);
        }

        private async Task PublishFundingStream(Reference fundingStream,
            SpecificationSummary specification,
            string jobId,
            Reference author,
            string correlationId,
            PrerequisiteCheckerType prerequisiteCheckerType,
            string[] batchProviderIds = null)
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

            IEnumerable<PublishedProvider> selectedPublishedProviders =
                batchProviderIds.IsNullOrEmpty() ?
                publishedProvidersForFundingStream.Values :
                publishedProvidersForFundingStream.Values.Where(_ => batchProviderIds.Contains(_.Current.ProviderId));

            _logger.Information($"Verifying prerequisites for funding publish");

            IPrerequisiteChecker prerequisiteChecker = _prerequisiteCheckerLocator.GetPreReqChecker(prerequisiteCheckerType);
            await prerequisiteChecker.PerformChecks(specification, jobId, selectedPublishedProviders?.ToList());
            _logger.Information($"Prerequisites for publish passed");

            TemplateMapping templateMapping = await GetTemplateMapping(fundingStream, specification.Id);

            PublishedFundingInput publishedFundingInput = await _publishedFundingService.GeneratePublishedFundingInput(publishedProvidersForFundingStream, 
                scopedPublishedProviders?.Values.Select(_ => _.Current.Provider), 
                fundingStream, 
                specification);

            using Transaction transaction = _transactionFactory.NewTransaction<PublishService>();
            try
            {
                // if any error occurs while updating or indexing then we need to re-index all published providers for consistency
                transaction.Enroll(async () =>
                {
                    await _publishedProviderVersionService.CreateReIndexJob(author, correlationId);
                });

                await SavePublishedProvidersAsReleased(jobId, author, selectedPublishedProviders, correlationId);

                _logger.Information($"Generating published funding");
                IEnumerable<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)> publishedFundingToSave =
                    _publishedFundingGenerator.GeneratePublishedFunding(publishedFundingInput, publishedProvidersForFundingStream?.Values).ToList();
                _logger.Information($"A total of {publishedFundingToSave.Count()} published funding versions created to save.");

                // if any error occurs while updating then we still need to run the indexer to be consistent
                transaction.Enroll(async () =>
                {
                    await _publishedIndexSearchResiliencePolicy.ExecuteAsync(() => _publishedFundingSearchRepository.RunIndexer());
                });

                // Save a version of published funding and set this version to current
                _logger.Information($"Saving published funding");
                await _publishedFundingStatusUpdateService.UpdatePublishedFundingStatus(publishedFundingToSave, author, PublishedFundingStatus.Released,jobId,correlationId);
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
            catch
            {
                await transaction.Compensate();

                throw;
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