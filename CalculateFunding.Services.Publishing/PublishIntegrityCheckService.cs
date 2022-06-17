using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using Microsoft.Azure.Storage;
using Microsoft.FeatureManagement;

namespace CalculateFunding.Services.Publishing
{
    public class PublishIntegrityCheckService : JobProcessingService, IPublishIntegrityCheckService
    {
        private readonly ILogger _logger;
        private readonly IPublishedFundingContentsPersistenceService _publishedFundingContentsPersistenceService;
        private readonly IPublishedProviderContentPersistenceService _publishedProviderContentsPersistenceService;
        private readonly ISearchRepository<PublishedFundingIndex> _publishedFundingSearchRepository;
        private readonly AsyncPolicy _publishedIndexSearchResiliencePolicy;
        private readonly ISpecificationService _specificationService;
        private readonly IProviderService _providerService;
        private readonly IPublishedFundingDataService _publishedFundingDataService;
        private readonly AsyncPolicy _publishingResiliencePolicy;
        private readonly ICalculationsApiClient _calculationsApiClient;
        private readonly AsyncPolicy _calculationsApiClientPolicy;
        private readonly IPoliciesService _policiesService;
        private readonly IPublishedProviderContentsGeneratorResolver _publishedProviderContentsGeneratorResolver;
        private readonly IPublishedFundingVersionDataService _publishedFundingVersionDataService;
        private readonly IFeatureManagerSnapshot _featureManager;
        private readonly IReleaseManagementRepository _releaseManagementRepository;
        private readonly IBlobClient _blobClient;
        private readonly IPublishedProviderContentChannelPersistenceService _publishedProviderContentChannelPersistenceService;
        private readonly IPublishedFundingContentsChannelPersistenceService _publishedFundingContentsChannelPersistenceService;
        private readonly IPublishedFundingRepository _publishedFundingRepository;

        public PublishIntegrityCheckService(IJobManagement jobManagement,
            ILogger logger,
            ISpecificationService specificationService,
            IProviderService providerService,
            IPublishedFundingContentsPersistenceService publishedFundingContentsPersistenceService,
            IPublishedProviderContentPersistenceService publishedProviderContentsPersistenceService,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPublishedFundingDataService publishedFundingDataService,
            IPoliciesService policiesService,
            ICalculationsApiClient calculationsApiClient,
            IPublishedFundingService publishedFundingService,
            IPublishedProviderContentsGeneratorResolver publishedProviderContentsGeneratorResolver,
            ISearchRepository<PublishedFundingIndex> publishedFundingSearchRepository,
            IPublishedFundingVersionDataService publishedFundingVersionDataService,
            IReleaseManagementRepository releaseManagementRepository,
            IFeatureManagerSnapshot featureManager,
            IBlobClient blobClient,
            IPublishedProviderContentChannelPersistenceService publishedProviderContentChannelPersistenceService,
            IPublishedFundingContentsChannelPersistenceService publishedFundingContentsChannelPersistenceService,
            IPublishedFundingRepository publishedFundingRepository) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(publishedFundingContentsPersistenceService, nameof(publishedFundingContentsPersistenceService));
            Guard.ArgumentNotNull(publishedProviderContentsPersistenceService, nameof(publishedProviderContentsPersistenceService));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(publishedFundingDataService, nameof(publishedFundingDataService));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(publishingResiliencePolicies.CalculationsApiClient, nameof(publishingResiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(publishedFundingService, nameof(publishedFundingService));
            Guard.ArgumentNotNull(publishedProviderContentsGeneratorResolver, nameof(publishedProviderContentsGeneratorResolver));
            Guard.ArgumentNotNull(publishedFundingVersionDataService, nameof(publishedFundingVersionDataService));
            Guard.ArgumentNotNull(publishedFundingSearchRepository, nameof(publishedFundingSearchRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedIndexSearchResiliencePolicy, nameof(publishingResiliencePolicies.PublishedIndexSearchResiliencePolicy));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(featureManager, nameof(featureManager));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(publishedProviderContentChannelPersistenceService, nameof(publishedProviderContentChannelPersistenceService));
            Guard.ArgumentNotNull(publishedFundingContentsChannelPersistenceService, nameof(publishedFundingContentsChannelPersistenceService));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));

            _logger = logger;
            _calculationsApiClient = calculationsApiClient;
            _calculationsApiClientPolicy = publishingResiliencePolicies.CalculationsApiClient;
            _specificationService = specificationService;
            _publishedFundingContentsPersistenceService = publishedFundingContentsPersistenceService;
            _publishedProviderContentsPersistenceService = publishedProviderContentsPersistenceService;
            _providerService = providerService;
            _publishedFundingDataService = publishedFundingDataService;
            _publishingResiliencePolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _policiesService = policiesService;
            _publishedProviderContentsGeneratorResolver = publishedProviderContentsGeneratorResolver;
            _publishedFundingVersionDataService = publishedFundingVersionDataService;
            _publishedIndexSearchResiliencePolicy = publishingResiliencePolicies.PublishedIndexSearchResiliencePolicy;
            _publishedFundingSearchRepository = publishedFundingSearchRepository;
            _releaseManagementRepository = releaseManagementRepository;
            _featureManager = featureManager;
            _blobClient = blobClient;
            _publishedProviderContentChannelPersistenceService = publishedProviderContentChannelPersistenceService;
            _publishedFundingContentsChannelPersistenceService = publishedFundingContentsChannelPersistenceService;
            _publishedFundingRepository = publishedFundingRepository;
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            bool publishAll = SetPublishAll(message);
            bool isChannelsEnabled = await _featureManager.IsEnabledAsync("EnableReleaseManagementBackend");
            IEnumerable<string> batchProviders = SetBatchProviders(message);

            string specificationId = message.UserProperties["specification-id"] as string;
            SpecificationSummary specification = await SetSpecificationSummary(specificationId);

            Reference fundingStream = specification.FundingStreams.First();
            Reference fundingPeriod = specification.FundingPeriod;

            IEnumerable<PublishedFundingVersion> publishedFundingVersions = publishAll
                ? await _publishedFundingVersionDataService.GetPublishedFundingVersion(fundingStream.Id,
                    fundingPeriod.Id)
                : (await _publishingResiliencePolicy.ExecuteAsync(() =>
                    _publishedFundingDataService.GetCurrentPublishedFunding(fundingStream.Id,
                        fundingPeriod.Id))).Select(_ => _.Current);

            TemplateMapping templateMapping = await GetTemplateMapping(fundingStream, specification.Id);

            await CheckLegacyBlobs(fundingStream, specification, batchProviders, fundingPeriod, publishedFundingVersions, templateMapping, publishAll);

            if (isChannelsEnabled)
            {
                IEnumerable<Channel> channels = (await _releaseManagementRepository.GetChannels()).ToList();

                await CheckFundingVersions(specificationId, publishedFundingVersions, channels, publishAll);
                await CheckProviderVersions(specificationId, batchProviders, channels, fundingStream, fundingPeriod, templateMapping, publishAll);
            }

            await _publishedIndexSearchResiliencePolicy.ExecuteAsync(() => _publishedFundingSearchRepository.RunIndexer());
        }

        private async Task CheckProviderVersions(string specificationId, IEnumerable<string> batchProviders,
            IEnumerable<Channel> channels,
            Reference fundingStream, Reference fundingPeriod, TemplateMapping templateMapping, bool publishAll)
        {
            Dictionary<Channel, List<PublishedProviderVersion>> providerVersionsToSave =
                new Dictionary<Channel, List<PublishedProviderVersion>>();

            IEnumerable<ReleasedProviderSummary> releasedProviderSummaries = publishAll
                ? await _releaseManagementRepository.GetReleasedProviderSummaryBySpecificationId(specificationId)
                : await _releaseManagementRepository.GetLatestReleasedProviderSummaryBySpecificationId(specificationId);

            IEnumerable<ReleasedProviderSummary> releasedProviderSummariesToSearch = batchProviders.IsNullOrEmpty()
                ? releasedProviderSummaries
                : releasedProviderSummaries.Where(p =>
                    batchProviders.Contains(p.ProviderId));

            foreach (ReleasedProviderSummary releasedProviderSummary in releasedProviderSummariesToSearch)
            {
                Channel channel = channels.Single(_ => _.ChannelId == releasedProviderSummary.ChannelId);
                string blobName = PublishedProviderChannelVersionService.GetBlobName(releasedProviderSummary.FundingId, channel.ChannelCode);

                try
                {
                    bool blobExists = await _blobClient.GetBlockBlobReference(blobName).ExistsAsync();
                    if (blobExists) continue;
                    if (!providerVersionsToSave.ContainsKey(channel))
                    {
                        providerVersionsToSave[channel] = new List<PublishedProviderVersion>();
                    }

                    IEnumerable<PublishedProviderVersion> releasedPublishedProviderVersions =
                        (await _publishingResiliencePolicy.ExecuteAsync(() =>
                            _publishedFundingRepository.GetPublishedProviderVersions(fundingStream.Id, fundingPeriod.Id,
                                releasedProviderSummary.ProviderId)))
                        .Where(_ => _.Status == PublishedProviderStatus.Released);

                    PublishedProviderVersion publishedProviderVersion =  releasedPublishedProviderVersions.SingleOrDefault(_ => _.FundingId == releasedProviderSummary.FundingId);

                    if (publishedProviderVersion != null)
                    {
                        providerVersionsToSave[channel]
                            .Add(publishedProviderVersion);
                    }
                }
                catch (StorageException ex)
                {
                    throw new RetriableException(ex.Message);
                }
            }

            foreach ((Channel channel, List<PublishedProviderVersion> providerVersionToSave) in providerVersionsToSave)
            {
                await _publishedProviderContentChannelPersistenceService.SavePublishedProviderContents(templateMapping,
                    providerVersionToSave, channel);
            }
        }

        private async Task CheckFundingVersions(string specificationId,
            IEnumerable<PublishedFundingVersion> publishedFundingVersions,
            IEnumerable<Channel> channels, bool publishAll)
        {
            IEnumerable<FundingGroupVersion> fundingGroupVersions = publishAll
                ? await _releaseManagementRepository.GetFundingGroupVersionsBySpecificationId(specificationId)
                : await _releaseManagementRepository.GetLatestFundingGroupVersionsBySpecificationId(specificationId);

            IEnumerable<FundingGroupVersion> fundingGroupVersionsToCheck = fundingGroupVersions.Where(f =>
                publishedFundingVersions.Select(_ => _.FundingId).Contains(f.FundingId));

            Dictionary<Channel, List<PublishedFundingVersion>> fundingVersionsToSave =
                new Dictionary<Channel, List<PublishedFundingVersion>>();

            foreach (FundingGroupVersion fundingGroupVersion in fundingGroupVersionsToCheck)
            {
                Channel channel = channels.Single(_ => _.ChannelId == fundingGroupVersion.ChannelId);

                PublishedFundingVersion publishedFunding =
                    publishedFundingVersions.SingleOrDefault(_ => _.FundingId == fundingGroupVersion.FundingId);

                if (publishedFunding == null)
                {
                    throw new NonRetriableException($"PublishedFundingVersion with FundingId = {fundingGroupVersion.FundingId} not found in list of publishedFundingVersions");
                }

                string blobName = PublishedFundingContentsChannelPersistenceService.GetBlobName(publishedFunding, channel);

                try
                {
                    bool blobExists = await _blobClient.GetBlockBlobReference(blobName).ExistsAsync();
                    if (blobExists) continue;
                    if (!fundingVersionsToSave.ContainsKey(channel))
                    {
                        fundingVersionsToSave[channel] = new List<PublishedFundingVersion>();
                    }

                    fundingVersionsToSave[channel].Add(publishedFunding);
                }
                catch (StorageException ex)
                {
                    throw new RetriableException(ex.Message);
                }
            }

            foreach ((Channel channel, List<PublishedFundingVersion> publishedFundingVersionsToSave) in fundingVersionsToSave)
            {
                await _publishedFundingContentsChannelPersistenceService.SavePublishedFundingContents(
                    publishedFundingVersionsToSave, channel);
            }
        }

        private async Task CheckLegacyBlobs(Reference fundingStream, SpecificationSummary specification,
            IEnumerable<string> batchProviders, Reference fundingPeriod, IEnumerable<PublishedFundingVersion> publishedFundingVersions,
            TemplateMapping templateMapping, bool publishAll)
        {
            (IDictionary<string, PublishedProvider> publishedProvidersForFundingStream,
                    IDictionary<string, PublishedProvider> _) =
                await _providerService.GetPublishedProviders(fundingStream,
                    specification);

            IDictionary<string, PublishedProvider> publishedProvidersByPublishedProviderId =
                publishedProvidersForFundingStream.Values.ToDictionary(_ => _.PublishedProviderId);

            IEnumerable<PublishedProvider> selectedPublishedProviders =
                batchProviders.IsNullOrEmpty()
                    ? publishedProvidersForFundingStream.Values
                    : batchProviders.Where(_ => publishedProvidersByPublishedProviderId.ContainsKey(_))
                        .Select(_ => publishedProvidersByPublishedProviderId[_]);

            TemplateMetadataContents templateMetadataContents =
                await _policiesService.GetTemplateMetadataContents(fundingStream.Id, fundingPeriod.Id,
                    specification.TemplateIds[fundingStream.Id]);

            if (templateMetadataContents == null)
            {
                throw new NonRetriableException(
                    $"Unable to get template metadata contents for funding stream. '{fundingStream.Id}'");
            }

            _logger.Information($"Saving published funding content to blob storage");
            await _publishedFundingContentsPersistenceService.SavePublishedFundingContents(publishedFundingVersions,
                templateMetadataContents);
            _logger.Information($"Finished saving published funding content to blob storage");

            IPublishedProviderContentsGenerator generator =
                _publishedProviderContentsGeneratorResolver.GetService(templateMetadataContents.SchemaVersion);

            _logger.Information($"Saving published provider content to blob storage");
            await _publishedProviderContentsPersistenceService.SavePublishedProviderContents(templateMetadataContents,
                templateMapping,
                selectedPublishedProviders, generator, publishAll);
            _logger.Information($"Finished saving published provider content to blob storage");
        }

        private async Task<SpecificationSummary> SetSpecificationSummary(string specificationId)
        {
            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);

            if (specification == null)
            {
                throw new NonRetriableException($"Could not find specification with id '{specificationId}'");
            }

            return specification;
        }

        private static IEnumerable<string> SetBatchProviders(Message message)
        {
            IEnumerable<string> batchProviders = null;

            if (message.UserProperties.ContainsKey("providers-batch"))
            {
                batchProviders = message.UserProperties["providers-batch"].ToString().AsPoco<IEnumerable<string>>();
            }

            return batchProviders;
        }

        private static bool SetPublishAll(Message message)
        {
            bool publishAll = false;
            if (message.UserProperties.ContainsKey("publish-all"))
            {
                publishAll = bool.Parse(message.UserProperties["publish-all"].ToString() ?? string.Empty);
            }

            return publishAll;
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
