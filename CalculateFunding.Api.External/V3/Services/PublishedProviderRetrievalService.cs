using AutoMapper;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Api.External.V3.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Polly;
using Serilog;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ProvidersApiClientModel = CalculateFunding.Common.ApiClient.Providers.Models;

namespace CalculateFunding.Api.External.V3.Services
{
    public class PublishedProviderRetrievalService : IPublishedProviderRetrievalService
    {
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly IProvidersApiClient _providersApiClient;
        private readonly AsyncPolicy _publishedFundingRepositoryPolicy;
        private readonly AsyncPolicy _providersApiClientPolicy;
        private readonly ILogger _logger;
        private readonly IMapper _mapper;
        private readonly IExternalApiFileSystemCacheSettings _cacheSettings;
        private readonly IFileSystemCache _fileSystemCache;

        public PublishedProviderRetrievalService(
            IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IProvidersApiClient providersApiClient,
            ILogger logger,
            IMapper mapper,
            IExternalApiFileSystemCacheSettings cacheSettings,
            IFileSystemCache fileSystemCache)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(providersApiClient, nameof(providersApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies.ProvidersApiClient, nameof(publishingResiliencePolicies.ProvidersApiClient));
            Guard.ArgumentNotNull(cacheSettings, nameof(cacheSettings));
            Guard.ArgumentNotNull(fileSystemCache, nameof(fileSystemCache));

            _publishedFundingRepository = publishedFundingRepository;
            _providersApiClient = providersApiClient;
            _logger = logger;
            _mapper = mapper;
            _cacheSettings = cacheSettings;
            _fileSystemCache = fileSystemCache;
            _publishedFundingRepositoryPolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _providersApiClientPolicy = publishingResiliencePolicies.ProvidersApiClient;
        }

        public async Task<IActionResult> GetPublishedProviderInformation(string publishedProviderVersion)
        {
            Guard.ArgumentNotNull(publishedProviderVersion, nameof(publishedProviderVersion));

            string blobName = $"{publishedProviderVersion}.json";

            ProviderVersionSystemCacheKey providerVersionFileSystemCacheKey = new ProviderVersionSystemCacheKey(blobName);

            if (_cacheSettings.IsEnabled && _fileSystemCache.Exists(providerVersionFileSystemCacheKey))
            {
                await using Stream providerVersionDocumentStream = _fileSystemCache.Get(providerVersionFileSystemCacheKey);

                ProviderVersionSearchResult cachedProviderVersionSearchResult = providerVersionDocumentStream.AsPoco<ProviderVersionSearchResult>();

                return new OkObjectResult(cachedProviderVersionSearchResult);
            }

            (string providerVersionId, string providerId) results = 
                await _publishedFundingRepositoryPolicy.ExecuteAsync(() => _publishedFundingRepository.GetPublishedProviderId(publishedProviderVersion));

            if (string.IsNullOrEmpty(results.providerVersionId) || string.IsNullOrEmpty(results.providerId))
            {
                _logger.Error($"Failed to retrieve published provider with publishedProviderVersion: {publishedProviderVersion}");
                
                return new NotFoundResult();
            }

            ApiResponse<ProvidersApiClientModel.Search.ProviderVersionSearchResult> apiResponse =
                await _providersApiClientPolicy.ExecuteAsync(() => 
                    _providersApiClient.GetProviderByIdFromProviderVersion(results.providerVersionId, results.providerId));

            if(apiResponse?.Content == null || !apiResponse.StatusCode.IsSuccess())
            {
                string errorMessage = $"Failed to retrieve GetProviderByIdFromProviderVersion with " +
                    $"providerVersionId: {results.providerVersionId} and providerId: {results.providerId}";

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

            return new OkObjectResult(providerVersionSearchResult);
        }
    }
}
