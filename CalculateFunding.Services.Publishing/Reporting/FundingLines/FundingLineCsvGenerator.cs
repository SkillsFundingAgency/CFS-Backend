using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class FundingLineCsvGenerator : IFundingLineCsvGenerator
    {
        private readonly ILogger _logger;
        private readonly IBlobClient _blobClient;
        private readonly IFundingLineCsvTransformServiceLocator _transformServiceLocator;
        private readonly IFundingLineCsvBatchProcessorServiceLocator _batchProcessorServiceLocator;
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IFileSystemCacheSettings _fileSystemCacheSettings;
        private readonly AsyncPolicy _blobClientPolicy;
        private readonly IJobTracker _jobTracker;

        public FundingLineCsvGenerator(IFundingLineCsvTransformServiceLocator transformServiceLocator,
            IPublishedFundingPredicateBuilder predicateBuilder,
            IBlobClient blobClient,
            IFileSystemAccess fileSystemAccess,
            IFileSystemCacheSettings fileSystemCacheSettings,
            IFundingLineCsvBatchProcessorServiceLocator batchProcessorServiceLocator,
            IPublishingResiliencePolicies policies,
            IJobTracker jobTracker,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobTracker, nameof(jobTracker));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(transformServiceLocator, nameof(transformServiceLocator));
            Guard.ArgumentNotNull(predicateBuilder, nameof(predicateBuilder));
            Guard.ArgumentNotNull(fileSystemAccess, nameof(fileSystemAccess));
            Guard.ArgumentNotNull(fileSystemCacheSettings, nameof(fileSystemCacheSettings));
            Guard.ArgumentNotNull(policies?.BlobClient, nameof(policies.BlobClient));
            Guard.ArgumentNotNull(batchProcessorServiceLocator, nameof(batchProcessorServiceLocator));

            _logger = logger;
            _batchProcessorServiceLocator = batchProcessorServiceLocator;
            _jobTracker = jobTracker;
            _blobClient = blobClient;
            _transformServiceLocator = transformServiceLocator;
            _fileSystemAccess = fileSystemAccess;
            _fileSystemCacheSettings = fileSystemCacheSettings;
            _blobClientPolicy = policies.BlobClient;
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
                string fundingLineCode = parameters.FundingLineCode;
                string fundingStreamId = parameters.FundingStreamId;
                string fundingPeriodId = parameters.FundingPeriodId;
                FundingLineCsvGeneratorJobType jobType = parameters.JobType;

                CsvFileInfo fileInfo = new CsvFileInfo(_fileSystemCacheSettings.Path,
                    jobType,
                    specificationId,
                    fundingLineCode,
                    fundingStreamId,
                    fundingPeriodId);

                string temporaryPath = fileInfo.TemporaryPath;
                
                EnsureFileIsNew(temporaryPath);
                
                IFundingLineCsvTransform fundingLineCsvTransform = _transformServiceLocator.GetService(jobType);
                IFundingLineCsvBatchProcessor fundingLineCsvBatchProcessor = _batchProcessorServiceLocator.GetService(jobType);

                bool processedResults = await fundingLineCsvBatchProcessor.GenerateCsv(jobType, 
                    specificationId, 
                    temporaryPath, 
                    fundingLineCsvTransform,
                    fundingLineCode,
                    fundingStreamId);

                if (!processedResults)
                {
                    _logger.Information(
                        $"Did not create a new csv report as no providers matched for the job type {jobType} in the specification {specificationId}" + 
                        $" and funding line code {fundingLineCode} and funding stream id {fundingStreamId}");

                    await CompleteJob(parameters);
                    
                    return;
                }

                ICloudBlob blob = _blobClient.GetBlockBlobReference(fileInfo.FileName);
                blob.Properties.ContentDisposition = fileInfo.ContentDisposition;

                using (Stream csvFileStream = _fileSystemAccess.OpenRead(temporaryPath))
                {
                    await _blobClientPolicy.ExecuteAsync(() => UploadBlob(blob, csvFileStream, parameters.ToDictionary()));
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

        private async Task UploadBlob(ICloudBlob blob, Stream csvFileStream, IDictionary<string, string> metadata)
        {
            await _blobClient.UploadAsync(blob, csvFileStream);
            await _blobClient.AddMetadataAsync(blob, metadata);
        }

        private async Task CompleteJob(JobParameters parameters)
        {
            await _jobTracker.CompleteTrackingJob(parameters.JobId);
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
            
            public string FundingLineCode { get; private set; }

            public string FundingStreamId { get; private set; }
            
            public string FundingPeriodId { get; private set; }

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
                    JobId = jobId,
                    FundingLineCode = GetProperty(message, "funding-line-code"),
                    FundingStreamId = GetProperty(message, "funding-stream-id"),
                    FundingPeriodId = GetProperty(message, "funding-period-id")
                };
            }
            public IDictionary<string, string> ToDictionary()
            {
                return new Dictionary<string, string>
                {
                    { "specification-id", SpecificationId },
                    { "job-type", JobType.ToString() },
                    { "jobId", JobId },
                    { "funding-line-code", FundingLineCode },
                    { "funding-stream-id", FundingStreamId },
                    { "funding-period-id", FundingPeriodId },
                    { "file-name", GetPrettyFileName(JobType, FundingLineCode, FundingStreamId, FundingPeriodId) }
                };
            }

            private static string GetProperty(Message message, string property)
            {
                return message.GetUserProperty<string>(property);
            }
        }

        private class CsvFileInfo
        {
            public CsvFileInfo(string root,
                FundingLineCsvGeneratorJobType jobType,
                string specificationId,
                string fundingLineCode,
                string fundingStreamId,
                string fundingPeriodId)
            {
                ContentDisposition = $"attachment; filename={GetPrettyFileName(jobType, fundingLineCode, fundingStreamId, fundingPeriodId)}";
                
                fundingLineCode = WithPrefixDelimiterOrEmpty(fundingLineCode);
                fundingStreamId = WithPrefixDelimiterOrEmpty(fundingStreamId);

                FileName = $"funding-lines-{specificationId}-{jobType}{fundingLineCode}{fundingStreamId}.csv";
                TemporaryPath = Path.Combine(root, FileName);
            }

            private string WithPrefixDelimiterOrEmpty(string literal) 
                => literal.IsNullOrWhitespace() ? string.Empty : $"-{literal}";
            
            public string FileName { get; }
            
            public string TemporaryPath { get; }
            
            public string ContentDisposition { get; }
        }

        private static string GetPrettyFileName(FundingLineCsvGeneratorJobType jobType, 
            string fundingLineCode, 
            string fundingStreamId, 
            string fundingPeriodId)
        {
            string utcNow = DateTimeOffset.UtcNow.ToString("s").Replace(":", null);
            
            switch (jobType)
            {
                case FundingLineCsvGeneratorJobType.CurrentState:
                    return $"{fundingStreamId} {fundingPeriodId} Provider Funding Lines Current State {utcNow}.csv";
                case FundingLineCsvGeneratorJobType.Released:
                    return $"{fundingStreamId} {fundingPeriodId} Provider Funding Lines Released Only {utcNow}.csv";
                case FundingLineCsvGeneratorJobType.History:
                    return $"{fundingStreamId} {fundingPeriodId} Provider Funding Lines All Versions {utcNow}.csv";
                case FundingLineCsvGeneratorJobType.HistoryProfileValues:
                    return $"{fundingStreamId} {fundingPeriodId} {fundingLineCode} Profile All Versions {utcNow}.csv";
                case FundingLineCsvGeneratorJobType.CurrentProfileValues:
                    return $"{fundingStreamId} {fundingPeriodId} {fundingLineCode} Profile Current State {utcNow}.csv";
                case FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues:
                    return $"{fundingStreamId} {fundingPeriodId} Funding Lines Current State {utcNow}.csv";
                case FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues:
                    return $"{fundingStreamId} {fundingPeriodId} Funding Lines All Versions {utcNow}.csv";
                case FundingLineCsvGeneratorJobType.Undefined:
                case FundingLineCsvGeneratorJobType.HistoryPublishedProviderEstate:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}