using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public abstract class BasePublishingCsvGenerator : JobProcessingService, IPublishedProviderEstateCsvGenerator, IHealthChecker
    {
        private const string PublishedFundingReportContainerName = "publishingreports";

        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IBlobClient _blobClient;
        private readonly AsyncPolicy _blobClientPolicy;
        private readonly ICsvUtils _csvUtils;
        private readonly ILogger _logger;
        private readonly IFileSystemCacheSettings _fileSystemCacheSettings;
        private readonly IPublishedProviderCsvTransformServiceLocator _publishedProviderCsvTransformServiceLocator;

        protected abstract string JobDefinitionName { get; }

        protected BasePublishingCsvGenerator(
            IJobManagement jobManagement,
            IFileSystemAccess fileSystemAccess,
            IBlobClient blobClient,
            IPublishingResiliencePolicies policies,
            ICsvUtils csvUtils,
            ILogger logger,
            IFileSystemCacheSettings fileSystemCacheSettings,
            IPublishedProviderCsvTransformServiceLocator publishedProviderCsvTransformServiceLocator) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(fileSystemAccess, nameof(fileSystemAccess));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(policies?.BlobClient, nameof(policies.BlobClient));
            Guard.ArgumentNotNull(csvUtils, nameof(csvUtils));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(fileSystemCacheSettings, nameof(fileSystemCacheSettings));
            Guard.ArgumentNotNull(publishedProviderCsvTransformServiceLocator, nameof(publishedProviderCsvTransformServiceLocator));

            _fileSystemAccess = fileSystemAccess;
            _blobClient = blobClient;
            _blobClientPolicy = policies.BlobClient;
            _csvUtils = csvUtils;
            _fileSystemCacheSettings = fileSystemCacheSettings;
            _publishedProviderCsvTransformServiceLocator = publishedProviderCsvTransformServiceLocator;
            _logger = logger;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) blobHealth = await _blobClient.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishedProviderVersionService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = blobHealth.Ok, DependencyName = _blobClient.GetType().GetFriendlyName(), Message = blobHealth.Message });

            return health;
        }

        public override async Task Process(Message message)
        {
            string temporaryFilePath = GetCsvFilePath(_fileSystemCacheSettings.Path, message);

            EnsureFileIsNew(temporaryFilePath);

            IPublishedProviderCsvTransform publishedProviderCsvTransform = _publishedProviderCsvTransformServiceLocator.GetService(JobDefinitionName);
            bool processedResults = await GenerateCsv(message, temporaryFilePath, publishedProviderCsvTransform);

            if (!processedResults)
            {
                _logger.Information("Did not create a new csv report as no providers matched");

                return;
            }

            await UploadToBlob(temporaryFilePath, GetCsvFileName(message), GetContentDisposition(message), GetMetadata(message));
        }

        protected abstract string GetCsvFileName(Message message);

        protected abstract IDictionary<string, string> GetMetadata(Message message);

        protected abstract Task<bool> GenerateCsv(Message message, string temporaryFilePath, IPublishedProviderCsvTransform publishedProviderCsvTransform);

        protected void AppendCsvFragment(string temporaryFilePath, IEnumerable<ExpandoObject> csvRows, bool outputHeaders)
        {
            string csv = _csvUtils.AsCsv(csvRows, outputHeaders);

            _fileSystemAccess.Append(temporaryFilePath, csv)
                .GetAwaiter()
                .GetResult();
        }

        private void EnsureFileIsNew(string path)
        {
            if (_fileSystemAccess.Exists(path))
            {
                _fileSystemAccess.Delete(path);
            }
        }

        private async Task UploadToBlob(string temporaryFilePath, string blobPath, string contentDisposition, IDictionary<string, string> metadata)
        {
            ICloudBlob blob = _blobClient.GetBlockBlobReference(blobPath, PublishedFundingReportContainerName);
            blob.Properties.ContentDisposition = contentDisposition;            

            using (Stream csvFileStream = _fileSystemAccess.OpenRead(temporaryFilePath))
            {
                await _blobClientPolicy.ExecuteAsync(() => UploadBlob(blob, csvFileStream, metadata));
            }
        }

        private async Task UploadBlob(ICloudBlob blob, Stream csvFileStream, IDictionary<string, string> metadata)
        {
            await _blobClient.UploadFileAsync(blob, csvFileStream);
            await _blobClient.AddMetadataAsync(blob, metadata);
        }

        private string GetCsvFilePath(string rootPath, Message message)
        {
            return Path.Combine(rootPath, GetCsvFileName(message));
        }

        protected abstract string GetContentDisposition(Message message);
    }
}
