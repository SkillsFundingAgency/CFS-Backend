using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class FundingLineCsvGenerator : IFundingLineCsvGenerator
    {
        public const int BatchSize = 100;

        private readonly ILogger _logger;
        private readonly IBlobClient _blobClient;
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly IFundingLineCsvTransformServiceLocator _transformServiceLocator;
        private readonly IPublishedFundingPredicateBuilder _predicateBuilder;
        private readonly ICsvUtils _csvUtils;
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IFileSystemCacheSettings _fileSystemCacheSettings;
        private readonly Policy _blobClientPolicy;
        private readonly Policy _publishedFundingRepository;
        private readonly IJobTracker _jobTracker;

        public FundingLineCsvGenerator(IFundingLineCsvTransformServiceLocator transformServiceLocator,
            IPublishedFundingPredicateBuilder predicateBuilder,
            IBlobClient blobClient,
            IPublishedFundingRepository publishedFunding,
            ICsvUtils csvUtils,
            IFileSystemAccess fileSystemAccess,
            IFileSystemCacheSettings fileSystemCacheSettings,
            IPublishingResiliencePolicies policies,
            IJobTracker jobTracker,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobTracker, nameof(jobTracker));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(transformServiceLocator, nameof(transformServiceLocator));
            Guard.ArgumentNotNull(predicateBuilder, nameof(predicateBuilder));
            Guard.ArgumentNotNull(csvUtils, nameof(csvUtils));
            Guard.ArgumentNotNull(fileSystemAccess, nameof(fileSystemAccess));
            Guard.ArgumentNotNull(fileSystemCacheSettings, nameof(fileSystemCacheSettings));
            Guard.ArgumentNotNull(policies?.BlobClient, nameof(policies.BlobClient));
            Guard.ArgumentNotNull(policies?.PublishedFundingRepository, nameof(policies.PublishedFundingRepository));

            _logger = logger;
            _jobTracker = jobTracker;
            _blobClient = blobClient;
            _publishedFunding = publishedFunding;
            _transformServiceLocator = transformServiceLocator;
            _predicateBuilder = predicateBuilder;
            _csvUtils = csvUtils;
            _fileSystemAccess = fileSystemAccess;
            _fileSystemCacheSettings = fileSystemCacheSettings;
            _blobClientPolicy = policies.BlobClient;
            _publishedFundingRepository = policies.PublishedFundingRepository;
        }

        public async Task Run(Message message)
        {
            try
            {
                JobParameters parameters = message;

                if (!await _jobTracker.TryStartTrackingJob(parameters.JobId,
                    JobConstants.DefinitionNames.GeneratePublishedFundingCsvJob))
                {
                    throw new ArgumentOutOfRangeException(nameof(parameters.JobId));
                }

                string specificationId = parameters.SpecificationId;
                FundingLineCsvGeneratorJobType jobType = parameters.JobType;

                string temporaryFilePath = new CsvFilePath(_fileSystemCacheSettings.Path,
                    jobType,
                    specificationId);

                EnsureFileIsNew(temporaryFilePath);
                
                IFundingLineCsvTransform fundingLineCsvTransform = _transformServiceLocator.GetService(jobType);

                bool processedResults = jobType == FundingLineCsvGeneratorJobType.History ?
                    await GeneratePublishedProviderVersionCsv(specificationId, temporaryFilePath, fundingLineCsvTransform) :
                    await GeneratePublishedProviderCsv(jobType, specificationId, temporaryFilePath, fundingLineCsvTransform);

                if (!processedResults)
                {
                    _logger.Information(
                        $"Did not create a new csv report as no providers matched for the job type {jobType} in the specification {specificationId}");

                    await CompleteJob(parameters);
                    
                    return;
                }

                ICloudBlob blob = _blobClient.GetBlockBlobReference($"funding-lines-{jobType}-{specificationId}.csv");

                using (Stream csvFileStream = _fileSystemAccess.OpenRead(temporaryFilePath))
                {
                    await _blobClientPolicy.ExecuteAsync(() => _blobClient.UploadAsync(blob, csvFileStream));
                }

                await CompleteJob(parameters);
            }
            catch (Exception e)
            {
                const string error = "Unable to complete funding line csv generation job.";

                _logger.Error(e, error);

                throw new NonRetriableException(error);
            }
        }

        private async Task CompleteJob(JobParameters parameters)
        {
            await _jobTracker.CompleteTrackingJob(parameters.JobId);
        }

        private async Task<bool> GeneratePublishedProviderCsv(FundingLineCsvGeneratorJobType jobType,
            string specificationId, 
            string temporaryFilePath, 
            IFundingLineCsvTransform fundingLineCsvTransform)
        {
            bool outputHeaders = true;
            bool processedResults = false;

            string predicate = _predicateBuilder.BuildPredicate(jobType);

            await _publishedFundingRepository.ExecuteAsync(() => _publishedFunding.PublishedProviderBatchProcessing(predicate,
                specificationId,
                publishedProviders =>
                {
                    IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(publishedProviders);

                    AppendCsvFragment(temporaryFilePath, csvRows, outputHeaders);

                    outputHeaders = false;
                    processedResults = true;
                    return Task.CompletedTask;
                }, BatchSize)
            );
            
            return processedResults;
        }

        private async Task<bool> GeneratePublishedProviderVersionCsv(string specificationId, 
            string temporaryFilePath, 
            IFundingLineCsvTransform fundingLineCsvTransform)
        {
            bool outputHeaders = true;
            bool processedResults = false;

            await _publishedFundingRepository.ExecuteAsync(() => _publishedFunding.PublishedProviderVersionBatchProcessing(specificationId,
                publishedProviderVersions =>
                {
                    IEnumerable<ExpandoObject> csvRows = fundingLineCsvTransform.Transform(publishedProviderVersions);

                    AppendCsvFragment(temporaryFilePath, csvRows, outputHeaders);

                    outputHeaders = false;
                    processedResults = true;
                    return Task.CompletedTask;
                }, BatchSize)
            );
            
            return processedResults;
        }

        private void AppendCsvFragment(string temporaryFilePath, IEnumerable<ExpandoObject> csvRows, bool outputHeaders)
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

        private class JobParameters
        {
            public string SpecificationId { get; private set; }
            
            public string JobId { get; private set; }

            public FundingLineCsvGeneratorJobType JobType { get; private set; }

            public static implicit operator JobParameters(Message message)
            {
                Guard.ArgumentNotNull(message, nameof(message));

                string specificationId = GetProperty(message, "specification-id");
                string jobType = GetProperty(message, "job-type");
                string jobId = GetProperty(message, "jobId");

                Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
                Guard.IsNullOrWhiteSpace(jobType, nameof(jobType));
                Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));
                
                return new JobParameters
                {
                    SpecificationId = specificationId,
                    JobType = jobType.AsEnum<FundingLineCsvGeneratorJobType>(),
                    JobId = jobId
                };
            }

            private static string GetProperty(Message message, string property)
            {
                return message.GetUserProperty<string>(property);
            }
        }

        private class CsvFilePath
        {
            private readonly string _root;
            private readonly FundingLineCsvGeneratorJobType _jobType;
            private readonly string _specificationId;

            public CsvFilePath(string root,
                FundingLineCsvGeneratorJobType jobType,
                string specificationId)
            {
                _root = root;
                _jobType = jobType;
                _specificationId = specificationId;
            }

            public static implicit operator string(CsvFilePath csvFilePath)
            {
                return Path.Combine(csvFilePath._root, $"funding-lines-{csvFilePath._jobType}-{csvFilePath._specificationId}.csv");
            }
        }
    }
}