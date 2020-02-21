using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Api.External.V3.Services
{
    public class ProviderFundingVersionService : IProviderFundingVersionService
    {
        private readonly IExternalApiFileSystemCacheSettings _cacheSettings;
        private readonly IFileSystemCache _fileSystemCache;
        private readonly IBlobClient _blobClient;
        private readonly Policy _blobClientPolicy;
        private readonly ILogger _logger;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly Policy _publishedFundingRepositoryPolicy;

        public ProviderFundingVersionService(IBlobClient blobClient,
            IPublishedFundingRepository publishedFundingRepository,
            ILogger logger,
            IExternalApiResiliencePolicies resiliencePolicies,
            IFileSystemCache fileSystemCache,
            IExternalApiFileSystemCacheSettings cacheSettings)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(fileSystemCache, nameof(fileSystemCache));
            Guard.ArgumentNotNull(cacheSettings, nameof(cacheSettings));
            Guard.ArgumentNotNull(resiliencePolicies.PublishedProviderBlobRepositoryPolicy, nameof(resiliencePolicies.PublishedProviderBlobRepositoryPolicy));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(resiliencePolicies.PublishedFundingRepositoryPolicy, nameof(resiliencePolicies.PublishedFundingRepositoryPolicy));

            _blobClient = blobClient;
            _logger = logger;
            _fileSystemCache = fileSystemCache;
            _cacheSettings = cacheSettings;
            _blobClientPolicy = resiliencePolicies.PublishedProviderBlobRepositoryPolicy;
            _publishedFundingRepository = publishedFundingRepository;
            _publishedFundingRepositoryPolicy = resiliencePolicies.PublishedFundingRepositoryPolicy;
        }

        public async Task<IActionResult> GetProviderFundingVersion(string providerFundingVersion)
        {
            if (string.IsNullOrWhiteSpace(providerFundingVersion)) return new BadRequestObjectResult("Null or empty id provided.");

            string blobName = $"{providerFundingVersion}.json";

            try
            {
                ProviderFundingFileSystemCacheKey cacheKey = new ProviderFundingFileSystemCacheKey(providerFundingVersion);

                if (_cacheSettings.IsEnabled && _fileSystemCache.Exists(cacheKey))
                {
                    using (Stream cachedStream = _fileSystemCache.Get(cacheKey))
                    {
                        return await GetContentResultForStream(cachedStream);
                    }
                }

                bool exists = await _blobClientPolicy.ExecuteAsync(() => _blobClient.BlobExistsAsync(blobName));

                if (!exists)
                {
                    _logger.Error($"Blob '{blobName}' does not exist.");

                    return new NotFoundResult();
                }

                ICloudBlob blob = await _blobClientPolicy.ExecuteAsync(() => _blobClient.GetBlobReferenceFromServerAsync(blobName));

                using (Stream blobStream = await _blobClientPolicy.ExecuteAsync(() => _blobClient.DownloadToStreamAsync(blob)))
                {
                    if (_cacheSettings.IsEnabled)
                    {
                        _fileSystemCache.Add(cacheKey, blobStream);
                    }

                    return await GetContentResultForStream(blobStream);
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to fetch blob '{blobName}' from azure storage";

                _logger.Error(ex, errorMessage);

                return new InternalServerErrorResult(errorMessage);
            }
        }

        public async Task<IActionResult> GetFundings(string publishedProviderVersion)
        {
            if (string.IsNullOrWhiteSpace(publishedProviderVersion)) return new BadRequestObjectResult("Null or empty id provided.");

            return new OkObjectResult(await _publishedFundingRepositoryPolicy.ExecuteAsync(() => _publishedFundingRepository.GetFundings(publishedProviderVersion)));
        }

        private async Task<ContentResult> GetContentResultForStream(Stream stream)
        {
            stream.Position = 0;

            using (StreamReader streamReader = new StreamReader(stream))
            {
                string template = await streamReader.ReadToEndAsync();

                return new ContentResult
                {
                    Content = template,
                    ContentType = "application/json",
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) blobHealth = await _blobClient.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderFundingVersionService),
                Dependencies =
                {
                    new DependencyHealth { HealthOk = blobHealth.Ok, DependencyName = _blobClient.GetType().GetFriendlyName(), Message = blobHealth.Message },
                }
            };

            return health;
        }
    }
}
