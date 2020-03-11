﻿using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public abstract class BasePublishingCsvGenerator : IPublishedProviderEstateCsvGenerator
    {
        private readonly IJobTracker _jobTracker;
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IBlobClient _blobClient;
        private readonly Policy _blobClientPolicy;
        private readonly ICsvUtils _csvUtils;
        private readonly ILogger _logger;
        private readonly IFileSystemCacheSettings _fileSystemCacheSettings;
        private readonly IPublishedProviderCsvTransformServiceLocator _publishedProviderCsvTransformServiceLocator;

        protected abstract string JobDefinitionName { get; }

        protected BasePublishingCsvGenerator(
            IJobTracker jobTracker,
            IFileSystemAccess fileSystemAccess,
            IBlobClient blobClient,
            IPublishingResiliencePolicies policies,
            ICsvUtils csvUtils,
            ILogger logger,
            IFileSystemCacheSettings fileSystemCacheSettings,
            IPublishedProviderCsvTransformServiceLocator publishedProviderCsvTransformServiceLocator)
        {
            Guard.ArgumentNotNull(jobTracker, nameof(jobTracker));
            Guard.ArgumentNotNull(fileSystemAccess, nameof(fileSystemAccess));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(policies?.BlobClient, nameof(policies.BlobClient));
            Guard.ArgumentNotNull(csvUtils, nameof(csvUtils));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(fileSystemCacheSettings, nameof(fileSystemCacheSettings));
            Guard.ArgumentNotNull(publishedProviderCsvTransformServiceLocator, nameof(publishedProviderCsvTransformServiceLocator));

            _jobTracker = jobTracker;
            _fileSystemAccess = fileSystemAccess;
            _blobClient = blobClient;
            _blobClientPolicy = policies.BlobClient;
            _csvUtils = csvUtils;
            _fileSystemCacheSettings = fileSystemCacheSettings;
            _publishedProviderCsvTransformServiceLocator = publishedProviderCsvTransformServiceLocator;
            _logger = logger;
        }

        public async Task Run(Message message)
        {
            try
            {
                string jobId = message.GetUserProperty<string>("jobId");

                if (!await _jobTracker.TryStartTrackingJob(jobId, JobDefinitionName))
                {
                    throw new ArgumentOutOfRangeException(nameof(jobId));
                }

                string temporaryFilePath = GetCsvFilePath(_fileSystemCacheSettings.Path, message);

                EnsureFileIsNew(temporaryFilePath);

                IPublishedProviderCsvTransform publishedProviderCsvTransform = _publishedProviderCsvTransformServiceLocator.GetService(JobDefinitionName);
                bool processedResults = await GenerateCsv(message, temporaryFilePath, publishedProviderCsvTransform);

                if (!processedResults)
                {
                    _logger.Information("Did not create a new csv report as no providers matched");

                    await CompleteJob(jobId);

                    return;
                }

                await UploadToBlob(temporaryFilePath, GetCsvFileName(message));
                await CompleteJob(jobId);
            }
            catch (Exception e)
            {
                const string error = "Unable to complete csv generation job.";

                _logger.Error(e, error);
                
                throw new NonRetriableException(error);
            }
        }

        protected abstract string GetCsvFileName(Message message);

        protected abstract Task<bool> GenerateCsv(Message message, string temporaryFilePath, IPublishedProviderCsvTransform publishedProviderCsvTransform);

        protected void AppendCsvFragment(string temporaryFilePath, IEnumerable<ExpandoObject> csvRows, bool outputHeaders)
        {
            string csv = _csvUtils.AsCsv(csvRows, outputHeaders);

            _fileSystemAccess.Append(temporaryFilePath, csv)
                .GetAwaiter()
                .GetResult();
        }

        private async Task CompleteJob(string jobId)
        {
            await _jobTracker.CompleteTrackingJob(jobId);
        }

        private void EnsureFileIsNew(string path)
        {
            if (_fileSystemAccess.Exists(path))
            {
                _fileSystemAccess.Delete(path);
            }
        }

        private async Task UploadToBlob(string temporaryFilePath, string blobPath)
        {
            ICloudBlob blob = _blobClient.GetBlockBlobReference(blobPath);

            using (Stream csvFileStream = _fileSystemAccess.OpenRead(temporaryFilePath))
            {
                await _blobClientPolicy.ExecuteAsync(() => _blobClient.UploadAsync(blob, csvFileStream));
            }
        }

        private string GetCsvFilePath(string rootPath, Message message)
        {
            return Path.Combine(rootPath, GetCsvFileName(message));
        }
    }
}