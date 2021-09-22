using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.FundingManagement;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Services
{
    public class ProviderFundingVersionService : IProviderFundingVersionService, IHealthChecker
    {
        private readonly IExternalApiFileSystemCacheSettings _cacheSettings;
        private readonly IFileSystemCache _fileSystemCache;
        private readonly IBlobClient _blobClient;
        private readonly AsyncPolicy _blobClientPolicy;
        private readonly ILogger _logger;
        private readonly IReleaseManagementRepository _releaseManagementRepository;
        private readonly IChannelUrlToIdResolver _channelUrlToIdResolver;

        public ProviderFundingVersionService(IBlobClient blobClient,
            IReleaseManagementRepository releaseManagementRepository,
            IChannelUrlToIdResolver channelUrlToIdResolver,
            ILogger logger,
            IExternalApiResiliencePolicies resiliencePolicies,
            IFileSystemCache fileSystemCache,
            IExternalApiFileSystemCacheSettings cacheSettings)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(channelUrlToIdResolver, nameof(channelUrlToIdResolver));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(fileSystemCache, nameof(fileSystemCache));
            Guard.ArgumentNotNull(cacheSettings, nameof(cacheSettings));
            Guard.ArgumentNotNull(resiliencePolicies.PublishedProviderBlobRepositoryPolicy, nameof(resiliencePolicies.PublishedProviderBlobRepositoryPolicy));

            _blobClient = blobClient;
            _logger = logger;
            _releaseManagementRepository = releaseManagementRepository;
            _channelUrlToIdResolver = channelUrlToIdResolver;
            _fileSystemCache = fileSystemCache;
            _cacheSettings = cacheSettings;
            _blobClientPolicy = resiliencePolicies.PublishedProviderBlobRepositoryPolicy;
        }

        public async Task<IActionResult> GetProviderFundingVersion(string channel, string providerFundingVersion)
        {
            if (string.IsNullOrWhiteSpace(providerFundingVersion)) return new BadRequestObjectResult("Null or empty id provided.");

            int? channelId = await _channelUrlToIdResolver.ResolveUrlToChannelId(channel);
            if (!channelId.HasValue)
            {
                return new PreconditionFailedResult("Channel does not exist");
            }

            bool providerVersionExists = await _releaseManagementRepository.ContainsProviderVersion(channelId.Value, providerFundingVersion);
            if (!providerVersionExists)
            {
                return new NotFoundObjectResult("Provider version not found.");
            }

            // TODO: Change to per channel once written to blob storage eg
            //string blobName = $"{channelId.Value}/{providerFundingVersion}.json";
            string blobName = $"{providerFundingVersion}.json";

            try
            {
                ProviderFundingFileSystemCacheKey cacheKey = new ProviderFundingFileSystemCacheKey(providerFundingVersion);

                if (_cacheSettings.IsEnabled && _fileSystemCache.Exists(cacheKey))
                {
                    using Stream cachedStream = _fileSystemCache.Get(cacheKey);
                    return GetResultStream(cachedStream);
                }

                bool exists = await _blobClientPolicy.ExecuteAsync(() => _blobClient.BlobExistsAsync(blobName));

                if (!exists)
                {
                    _logger.Error($"Blob '{blobName}' does not exist.");

                    return new NotFoundResult();
                }

                ICloudBlob blob = await _blobClientPolicy.ExecuteAsync(() => _blobClient.GetBlobReferenceFromServerAsync(blobName));

                using Stream blobStream = await _blobClientPolicy.ExecuteAsync(() => _blobClient.DownloadToStreamAsync(blob));

                if (_cacheSettings.IsEnabled)
                {
                    _fileSystemCache.Add(cacheKey, blobStream);
                }

                return GetResultStream(blobStream);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to fetch blob '{blobName}' from azure storage";

                _logger.Error(ex, errorMessage);

                return new InternalServerErrorResult(errorMessage);
            }
        }

        public async Task<IActionResult> GetFundings(string channel, string publishedProviderVersion)
        {
            if (string.IsNullOrWhiteSpace(publishedProviderVersion)) return new BadRequestObjectResult("Null or empty id provided.");

            int? channelId = await _channelUrlToIdResolver.ResolveUrlToChannelId(channel);

            IEnumerable<string> fundingGroupsVersionsForProvider = await _releaseManagementRepository.GetFundingGroupIdsForProviderFunding(channelId.Value, publishedProviderVersion);

            return new OkObjectResult(fundingGroupsVersionsForProvider);
        }

        private FileStreamResult GetResultStream(Stream stream)
        {
            stream.Position = 0;
            return new FileStreamResult(stream, "application/json");
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = await _blobClient.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderFundingVersionService),
                Dependencies =
                {
                    new DependencyHealth { HealthOk = Ok, DependencyName = _blobClient.GetType().GetFriendlyName(), Message = Message },
                }
            };

            return health;
        }
    }
}
