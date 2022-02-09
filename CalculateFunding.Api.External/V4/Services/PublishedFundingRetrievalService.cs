using CalculateFunding.Api.External.V4.Interfaces;
using CalculateFunding.Common.Helpers;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.External.V4;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Api.External.V4.Services
{
    public class PublishedFundingRetrievalService : IPublishedFundingRetrievalService, IHealthChecker
    {
        private readonly IBlobClient _blobClient;
        private readonly AsyncPolicy _publishedFundingRepositoryPolicy;
        private readonly IFileSystemCache _fileSystemCache;
        private readonly IBlobDocumentPathGenerator _blobDocumentPathGenerator;
        private readonly IExternalEngineOptions _externalEngineOptions;
        private readonly IExternalApiFileSystemCacheSettings _cacheSettings;
        private readonly ILogger _logger;

        public PublishedFundingRetrievalService(IBlobClient blobClient,
            IExternalApiResiliencePolicies resiliencePolicies,
            IFileSystemCache fileSystemCache,
            IBlobDocumentPathGenerator blobDocumentPathGenerator,
            ILogger logger,
            IExternalApiFileSystemCacheSettings cacheSettings,
            IExternalEngineOptions externalEngineOptions)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(fileSystemCache, nameof(fileSystemCache));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(blobDocumentPathGenerator, nameof(blobDocumentPathGenerator));
            Guard.ArgumentNotNull(cacheSettings, nameof(cacheSettings));
            Guard.ArgumentNotNull(externalEngineOptions, nameof(externalEngineOptions));
            Guard.ArgumentNotNull(resiliencePolicies.PublishedFundingBlobRepositoryPolicy, nameof(resiliencePolicies.PublishedFundingBlobRepositoryPolicy));

            _blobClient = blobClient;
            _publishedFundingRepositoryPolicy = resiliencePolicies.PublishedFundingBlobRepositoryPolicy;
            _fileSystemCache = fileSystemCache;
            _blobDocumentPathGenerator = blobDocumentPathGenerator;
            _logger = logger;
            _cacheSettings = cacheSettings;
            _externalEngineOptions = externalEngineOptions;
        }

        public async Task<Stream> GetFundingFeedDocument(string fundingId,
            string channelCode,
            bool isForPreLoad = false)
        {
            Guard.IsNullOrWhiteSpace(fundingId, nameof(fundingId));

            FundingFileSystemCacheKey fundingFileSystemCacheKey = _blobDocumentPathGenerator.GenerateFilesystemCacheKeyForFundingDocument(fundingId, channelCode);

            if (_cacheSettings.IsEnabled && _fileSystemCache.Exists(fundingFileSystemCacheKey))
            {
                if (isForPreLoad) return null;

                return _fileSystemCache.Get(fundingFileSystemCacheKey);
            }

            string blobDocumentPath = _blobDocumentPathGenerator.GenerateBlobPathForFundingDocument(fundingId, channelCode);

            ICloudBlob blob = _blobClient.GetBlockBlobReference(blobDocumentPath);

            if (!blob.Exists())
            {
                _logger.Error($"Failed to find blob with path: {blobDocumentPath}");
                return null;
            }

            Stream fundingDocumentStream = await _publishedFundingRepositoryPolicy.ExecuteAsync(() => _blobClient.DownloadToStreamAsync(blob));
            if (fundingDocumentStream == null || fundingDocumentStream.Length == 0)
            {
                _logger.Error($"Invalid blob returned: {blobDocumentPath}");
                return null;
            }

            if (_cacheSettings.IsEnabled && !_fileSystemCache.Exists(fundingFileSystemCacheKey))
            {
                _fileSystemCache.Add(fundingFileSystemCacheKey, fundingDocumentStream);
            }

            fundingDocumentStream.Position = 0;

            return isForPreLoad ? null : fundingDocumentStream;

        }

        public async Task<IDictionary<ExternalFeedFundingGroupItem, Stream>> GetFundingFeedDocuments(IEnumerable<ExternalFeedFundingGroupItem> batchItems, string channelCode, CancellationToken cancellationToken)
        {
            ConcurrentDictionary<ExternalFeedFundingGroupItem, Stream> feedContentResults = new ConcurrentDictionary<ExternalFeedFundingGroupItem, Stream>(_externalEngineOptions.BlobLookupConcurrencyCount, batchItems.Count());

            List<Task> allTasks = new List<Task>(batchItems.Count());
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _externalEngineOptions.BlobLookupConcurrencyCount);
            foreach (ExternalFeedFundingGroupItem item in batchItems)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        try
                        {
                            Stream contents = await GetFundingFeedDocument(item.FundingId, channelCode);
                            feedContentResults.TryAdd(item, contents);

                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }

            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            return EnsureOrderedReturnOfItemsBasedOnInput(batchItems, feedContentResults);
        }

        private static IDictionary<ExternalFeedFundingGroupItem, Stream> EnsureOrderedReturnOfItemsBasedOnInput(IEnumerable<ExternalFeedFundingGroupItem> batchItems, ConcurrentDictionary<ExternalFeedFundingGroupItem, Stream> feedContentResults)
        {
            Dictionary<ExternalFeedFundingGroupItem, Stream> result = new Dictionary<ExternalFeedFundingGroupItem, Stream>(batchItems.Count());
            foreach (ExternalFeedFundingGroupItem item in batchItems)
            {
                result.Add(item, feedContentResults[item]);
            }

            return result;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = await _blobClient.IsHealthOk();

            return new ServiceHealth()
            {
                Name = nameof(ProviderFundingVersionService),
                Dependencies =
                {
                    new DependencyHealth { HealthOk = Ok, DependencyName = _blobClient.GetType().GetFriendlyName(), Message = Message },
                }
            };
        }
    }
}
