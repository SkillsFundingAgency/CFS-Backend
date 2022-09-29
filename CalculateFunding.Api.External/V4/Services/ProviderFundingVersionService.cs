using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.FundingManagement;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
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
        private readonly IChannelUrlToChannelResolver _channelUrlToChannelResolver;
        private readonly IBlobDocumentPathGenerator _blobDocumentPathGenerator;

        public ProviderFundingVersionService(IBlobClient blobClient,
            IReleaseManagementRepository releaseManagementRepository,
            IChannelUrlToChannelResolver channelUrlToChannelResolver,
            IBlobDocumentPathGenerator blobDocumentPathGenerator,
            ILogger logger,
            IExternalApiResiliencePolicies resiliencePolicies,
            IFileSystemCache fileSystemCache,
            IExternalApiFileSystemCacheSettings cacheSettings)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(channelUrlToChannelResolver, nameof(channelUrlToChannelResolver));
            Guard.ArgumentNotNull(blobDocumentPathGenerator, nameof(blobDocumentPathGenerator));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(fileSystemCache, nameof(fileSystemCache));
            Guard.ArgumentNotNull(cacheSettings, nameof(cacheSettings));
            Guard.ArgumentNotNull(resiliencePolicies.PublishedProviderBlobRepositoryPolicy, nameof(resiliencePolicies.PublishedProviderBlobRepositoryPolicy));

            _blobClient = blobClient;
            _logger = logger;
            _releaseManagementRepository = releaseManagementRepository;
            _blobDocumentPathGenerator = blobDocumentPathGenerator;
            _channelUrlToChannelResolver = channelUrlToChannelResolver;
            _fileSystemCache = fileSystemCache;
            _cacheSettings = cacheSettings;
            _blobClientPolicy = resiliencePolicies.PublishedProviderBlobRepositoryPolicy;
        }

        public async Task<IActionResult> GetProviderFundingVersion(string channelUrl, string providerFundingVersion)
        {
            if (string.IsNullOrWhiteSpace(providerFundingVersion)) return new BadRequestObjectResult("Null or empty id provided.");

            Channel channel = await _channelUrlToChannelResolver.ResolveUrlToChannel(channelUrl);
            if (channel == null)
            {
                return new PreconditionFailedResult("Channel does not exist");
            }

            bool providerVersionExists = await _releaseManagementRepository.ContainsProviderVersion(channel.ChannelId, providerFundingVersion);
            if (!providerVersionExists)
            {
                return new NotFoundObjectResult("Provider version not found.");
            }

            string blobName = _blobDocumentPathGenerator.GenerateBlobPathForFundingDocument(providerFundingVersion, channel.ChannelCode);

            try
            {
                ProviderFundingFileSystemCacheKey cacheKey = _blobDocumentPathGenerator.GenerateFilesystemCacheKeyForProviderFundingDocument(providerFundingVersion, channel.ChannelCode);

                if (_cacheSettings.IsEnabled && _fileSystemCache.Exists(cacheKey))
                {
                    Stream cachedStream = _fileSystemCache.Get(cacheKey);
                    var cachedContent = _channelUrlToChannelResolver.GetContentWithChannelProviderVersion(cachedStream, channel.ChannelCode).Result;
                    return GetResultStream(cachedContent);
                }

                bool exists = await _blobClientPolicy.ExecuteAsync(() => _blobClient.BlobExistsAsync(blobName));

                if (!exists)
                {
                    _logger.Error($"Blob '{blobName}' does not exist.");

                    return new NotFoundResult();
                }

                ICloudBlob blob = await _blobClientPolicy.ExecuteAsync(() => _blobClient.GetBlobReferenceFromServerAsync(blobName));

                Stream blobStream = await _blobClientPolicy.ExecuteAsync(() => _blobClient.DownloadToStreamAsync(blob));

                Stream content = _channelUrlToChannelResolver.GetContentWithChannelProviderVersion(blobStream, channel.ChannelCode).Result;

                if (_cacheSettings.IsEnabled)
                {
                    _fileSystemCache.Add(cacheKey, content);
                }               
                return GetResultStream(content);
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to fetch blob '{blobName}' from azure storage";

                _logger.Error(ex, errorMessage);

                return new InternalServerErrorResult(errorMessage);
            }
        }

        public async Task<IActionResult> GetFundings(string channelUrl, string publishedProviderVersion)
        {
            if (string.IsNullOrWhiteSpace(publishedProviderVersion)) return new BadRequestObjectResult("Null or empty id provided.");

            Channel channel = await _channelUrlToChannelResolver.ResolveUrlToChannel(channelUrl);
            
            if (channel == null)
            {
                return new PreconditionFailedResult("Channel does not exist");
            }

            IEnumerable<string> fundingGroupsVersionsForProvider = await _releaseManagementRepository.GetFundingGroupIdsForProviderFunding(channel.ChannelId, publishedProviderVersion);

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
