using AutoMapper;
using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Api.External.V4.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Serilog;
using System.IO;
using System.Threading.Tasks;
using ProvidersApiClientModel = CalculateFunding.Common.ApiClient.Providers.Models;

namespace CalculateFunding.Api.External.V4.Services
{
    public class PublishedProviderRetrievalService : IPublishedProviderRetrievalService
    {
        private readonly IReleaseManagementRepository _releaseManagementRepository;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IExternalApiFileSystemCacheSettings _cacheSettings;
        private readonly IFileSystemCache _fileSystemCache;
        private readonly IProvidersApiClient _providersApiClient;
        private readonly AsyncPolicy _providersApiClientPolicy;
        private readonly IChannelUrlToChannelResolver _channelUrlToChannelResolver;

        public PublishedProviderRetrievalService(
            IReleaseManagementRepository releaseManagementRepository,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IProvidersApiClient providersApiClient,
            ILogger logger,
            IMapper mapper,
            IExternalApiFileSystemCacheSettings cacheSettings,
            IChannelUrlToChannelResolver channelUrlToChannelResolver,
            IFileSystemCache fileSystemCache)
        {
            Guard.ArgumentNotNull(providersApiClient, nameof(providersApiClient));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(channelUrlToChannelResolver, nameof(channelUrlToChannelResolver));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(publishingResiliencePolicies.ProvidersApiClient, nameof(publishingResiliencePolicies.ProvidersApiClient));
            Guard.ArgumentNotNull(cacheSettings, nameof(cacheSettings));
            Guard.ArgumentNotNull(fileSystemCache, nameof(fileSystemCache));

            _releaseManagementRepository = releaseManagementRepository;
            _providersApiClient = providersApiClient;
            _logger = logger;
            _mapper = mapper;
            _cacheSettings = cacheSettings;
            _fileSystemCache = fileSystemCache;
            _providersApiClientPolicy = publishingResiliencePolicies.ProvidersApiClient;
            _channelUrlToChannelResolver = channelUrlToChannelResolver;
        }

        public async Task<ActionResult<ProviderVersionSearchResult>> GetPublishedProviderInformation(string channelKey, string publishedProviderVersion)
        {
            Guard.IsNullOrWhiteSpace(channelKey, nameof(channelKey));
            Guard.IsNullOrWhiteSpace(publishedProviderVersion, nameof(publishedProviderVersion));

            Channel channel = await _channelUrlToChannelResolver.ResolveUrlToChannel(channelKey);

            if (channel == null)
            {
                return new PreconditionFailedResult("Channel not found");
            }

            ProviderVersionSystemCacheKey providerVersionFileSystemCacheKey = new ProviderVersionSystemCacheKey($"{channel.ChannelCode}_{publishedProviderVersion}");

            if (_cacheSettings.IsEnabled && _fileSystemCache.Exists(providerVersionFileSystemCacheKey))
            {
                await using Stream providerVersionDocumentStream = _fileSystemCache.Get(providerVersionFileSystemCacheKey);

                ProviderVersionSearchResult cachedProviderVersionSearchResult = providerVersionDocumentStream.AsPoco<ProviderVersionSearchResult>();

                return cachedProviderVersionSearchResult;
            }

            ProviderVersionInChannel releasedProvider = await _releaseManagementRepository.GetReleasedProvider(publishedProviderVersion, channel.ChannelId);

            if (string.IsNullOrEmpty(releasedProvider?.ProviderId))
            {
                _logger.Error($"Failed to retrieve published provider with publishedProviderVersion: {publishedProviderVersion}");

                return new NotFoundResult();
            }

            ApiResponse<ProvidersApiClientModel.Search.ProviderVersionSearchResult> apiResponse =
                await _providersApiClientPolicy.ExecuteAsync(() =>
                    _providersApiClient.GetProviderByIdFromProviderVersion(releasedProvider.CoreProviderVersionId, releasedProvider.ProviderId));

            if (apiResponse?.Content == null || !apiResponse.StatusCode.IsSuccess())
            {
                string errorMessage = $"Failed to retrieve GetProviderByIdFromProviderVersion with " +
                    $"providerVersionId: {releasedProvider.CoreProviderVersionId} and providerId: {releasedProvider.ProviderId}";

                _logger.Error(errorMessage);

                return new InternalServerErrorResult(errorMessage);
            }

            ProviderVersionSearchResult providerVersionSearchResult = _mapper.Map<ProviderVersionSearchResult>(apiResponse.Content);

            if (_cacheSettings.IsEnabled)
            {
                if (!_fileSystemCache.Exists(providerVersionFileSystemCacheKey))
                {
                    await using MemoryStream stream = new MemoryStream(providerVersionSearchResult.AsJsonBytes());

                    _fileSystemCache.Add(providerVersionFileSystemCacheKey, stream, ensureFolderExists: true);
                }
            }

            return providerVersionSearchResult;
        }
    }
}
