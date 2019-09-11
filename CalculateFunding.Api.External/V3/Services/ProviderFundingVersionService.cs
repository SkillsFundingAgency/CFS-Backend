﻿using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Api.External.V3.Interfaces;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Api.External.V3.Services
{
    public class ProviderFundingVersionService : IProviderFundingVersionService
    {
        private readonly IFileSystemCache _fileSystemCache;
        private readonly IBlobClient _blobClient;
        private readonly Policy _blobClientPolicy;
        private readonly ILogger _logger;

        public ProviderFundingVersionService(IBlobClient blobClient,
            ILogger logger,
            IExternalApiResiliencePolicies resiliencePolicies,
            IFileSystemCache fileSystemCache)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(fileSystemCache, nameof(fileSystemCache));

            _blobClient = blobClient;
            _logger = logger;
            _fileSystemCache = fileSystemCache;
            _blobClientPolicy = resiliencePolicies.BlobRepositoryPolicy;
        }

        public async Task<IActionResult> GetProviderFundingVersion(string providerFundingVersion)
        {
            if (string.IsNullOrWhiteSpace(providerFundingVersion)) return new BadRequestObjectResult("Null or empty id provided.");

            string blobName = $"{providerFundingVersion}.json";

            try
            {
                ProviderFileSystemCacheKey cacheKey = new ProviderFileSystemCacheKey(providerFundingVersion);

                if (_fileSystemCache.Exists(cacheKey))
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
                    _fileSystemCache.Add(cacheKey, blobStream);

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