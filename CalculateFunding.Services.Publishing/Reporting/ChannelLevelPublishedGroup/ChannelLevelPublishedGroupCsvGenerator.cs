using System;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Models;
using Microsoft.Azure.Storage.Blob;
using System.IO;
using Polly;
using CalculateFunding.Services.Publishing.Reporting.ChannelLevelPublishedGroup;
using Microsoft.Azure.Amqp.Framing;

namespace CalculateFunding.Services.Publishing.Reporting.PublishedProviderState
{
    public class ChannelLevelPublishedGroupCsvGenerator : BaseChannelLevelPublishedGroupCsvGenerator
    {
        private const string PublishedFundingReportContainerName = "publishingreports";

        private const string _channelCodeString = "<channelCode>";

        public const int BatchSize = 1000;

        private readonly ILogger _logger;

        private readonly IBlobClient _blobClient;

        private readonly IFileSystemAccess _fileSystemAccess;

        private readonly IPublishedProvidersSearchService _publishedProvidersSearchService;

        private readonly IProviderService _providerService;

        private readonly ISpecificationService _specificationService;

        private readonly IPoliciesApiClient _policiesApiClient;

        private readonly IFileSystemCacheSettings _fileSystemCacheSettings;

        private readonly IFundingLineCsvTransformServiceLocator _transformServiceLocator;

        private readonly IChannelLevelPublishedGroupsCsvBatchProcessorServiceLocator _processorServiceLocator;

        private readonly AsyncPolicy _blobClientPolicy;

        protected override string JobDefinitionName => JobConstants.DefinitionNames.GeneratePublishedProviderStateSummaryCsvJob;

        public ChannelLevelPublishedGroupCsvGenerator(
            IJobManagement jobManagement,
            IFileSystemAccess fileSystemAccess,
            IFileSystemCacheSettings fileSystemCacheSettings,
            IBlobClient blobClient,
            IPoliciesApiClient policiesApiClient,
            IPublishedProvidersSearchService publishedProvidersSearchService,
            IProviderService providerService,
            ISpecificationService specificationService,
            ICsvUtils csvUtils,
            ILogger logger,
            IPublishingResiliencePolicies policies,
            IChannelLevelPublishedGroupsCsvBatchProcessorServiceLocator channelLevelPublishedGroupsCsvBatchProcessorServiceLocator,
            IFundingLineCsvTransformServiceLocator transformServiceLocator)
            : base(jobManagement, fileSystemAccess, blobClient, policies, csvUtils, logger, fileSystemCacheSettings, channelLevelPublishedGroupsCsvBatchProcessorServiceLocator, transformServiceLocator)
        {
            Guard.ArgumentNotNull(publishedProvidersSearchService, nameof(publishedProvidersSearchService));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));

            _publishedProvidersSearchService = publishedProvidersSearchService;
            _providerService = providerService;
            _specificationService = specificationService;
            _policiesApiClient = policiesApiClient;
            _fileSystemCacheSettings = fileSystemCacheSettings;
            _transformServiceLocator = transformServiceLocator;
            _logger = logger;
            _blobClient = blobClient;
            _fileSystemAccess = fileSystemAccess;
            _blobClientPolicy = policies.BlobClient;
            _processorServiceLocator = channelLevelPublishedGroupsCsvBatchProcessorServiceLocator;
        }

        protected override async Task<bool> GenerateCsv(Message message,
            IFundingLineCsvTransform channelLevelPublishedGroupCsvTransform)
        {
            bool processedResults = false;
            JobParameters parameters = message;

            string specificationId = parameters.SpecificationId;
            string fundingLineName = parameters.FundingLineName;
            string fundingStreamId = parameters.FundingStreamId;
            string fundingLineCode = parameters.FundingLineCode;
            string fundingPeriodId = parameters.FundingPeriodId;
            FundingLineCsvGeneratorJobType jobType = parameters.JobType;

            IFundingLineCsvTransform fundingLineCsvTransform = _transformServiceLocator.GetService(jobType);
            IFundingLineCsvBatchProcessor fundingLineCsvBatchProcessor = _processorServiceLocator.GetService(jobType);

            CsvFileInfo fileInfo = new CsvFileInfo(_fileSystemCacheSettings.Path,
                jobType,
                specificationId,
                fundingLineName,
                fundingStreamId,
                fundingPeriodId,
                fundingLineCode);

            string temporaryPath = fileInfo.TemporaryPath;
            foreach (string channelCode in Enum.GetNames(typeof(ChannelType)))
            {
                string tempPath = temporaryPath.Replace(_channelCodeString, channelCode);
                EnsureFileIsNew(tempPath);
            }

            processedResults = await fundingLineCsvBatchProcessor.GenerateCsv(jobType,
                specificationId,
                fundingPeriodId,
                temporaryPath,
                fundingLineCsvTransform,
                fundingLineName,
                fundingStreamId,
                fundingLineCode);

            if (!processedResults)
            {
                _logger.Information(
                    $"Did not create a new csv report as no providers matched for the job type {jobType} in the specification {specificationId}" +
                    $" and funding line code {fundingLineName} , funding stream id {fundingStreamId} ");
            }

            foreach (string channelCode in Enum.GetNames(typeof(ChannelType)))
            {
                string tempPath = temporaryPath.Replace(_channelCodeString, channelCode);
                if (_fileSystemAccess.Exists(tempPath))
                {
                    string tempFileName = fileInfo.FileName.Replace(_channelCodeString, channelCode);
                    ICloudBlob blob = _blobClient.GetBlockBlobReference(tempFileName, PublishedFundingReportContainerName);
                    blob.Properties.ContentDisposition = $"attachment; filename={GetPrettyFileName(jobType, fundingLineName, fundingStreamId, fundingPeriodId, channelCode)}".ToASCII();

                    await using Stream csvFileStream = _fileSystemAccess.OpenRead(tempPath);

                    await _blobClientPolicy.ExecuteAsync(() => UploadBlob(blob, csvFileStream, parameters.ToDictionary(channelCode)));
                    processedResults = true;
                }
            }

            return processedResults;
        }

        private async Task UploadBlob(ICloudBlob blob, Stream csvFileStream, IDictionary<string, string> metadata)
        {
            await _blobClient.UploadFileAsync(blob, csvFileStream);
            await _blobClient.AddMetadataAsync(blob, metadata.ToDictionary(_ => _.Key, _ => _.Value.ToASCII()));
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

            private string JobId { get; set; }

            public string FundingLineName { get; private set; }
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
                    FundingLineName = GetProperty(message, "funding-line-name"),
                    FundingStreamId = GetProperty(message, "funding-stream-id"),
                    FundingPeriodId = GetProperty(message, "funding-period-id"),
                    FundingLineCode = GetProperty(message, "funding-line-code")
                };
            }

            public IDictionary<string, string> ToDictionary(string channelCode)
            {
                return new Dictionary<string, string>
                {
                    { "specification-id", SpecificationId },
                    { "job-type", JobType.ToString() },
                    { "jobId", JobId },
                    { "funding-line-code", FundingLineName },
                    { "funding-stream-id", FundingStreamId },
                    { "funding-period-id", FundingPeriodId },
                    { "channel-code", channelCode },
                    { "file-name", GetPrettyFileName(JobType, FundingLineName, FundingStreamId, FundingPeriodId, channelCode) }
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
                string fundingLineName,
                string fundingStreamId,
                string fundingPeriodId,
                string channelCode)
            {
                ContentDisposition = $"attachment; filename={GetPrettyFileName(jobType, fundingLineName, fundingStreamId, fundingPeriodId, channelCode)}".ToASCII();

                fundingLineName = WithPrefixDelimiterOrEmpty(fundingLineName);
                fundingStreamId = WithPrefixDelimiterOrEmpty(fundingStreamId);

                FileName = $"funding-lines-{specificationId}-{jobType}{fundingLineName}{fundingStreamId}-{_channelCodeString}.csv".ToASCII();
                TemporaryPath = Path.Combine(root, FileName);
            }

            private string WithPrefixDelimiterOrEmpty(string literal)
                => literal.IsNullOrWhitespace() ? string.Empty : $"-{literal}";

            public string FileName { get; }

            public string TemporaryPath { get; }

            public string ContentDisposition { get; set; }
        }

        private static string GetPrettyFileName(FundingLineCsvGeneratorJobType jobType,
            string fundingLineCode,
            string fundingStreamId,
            string fundingPeriodId,
            string channelCode)
        {
            string utcNow = DateTimeOffset.UtcNow.ToString("s").Replace(":", null);

            return jobType switch
            {
                FundingLineCsvGeneratorJobType.CurrentState => $"{fundingStreamId} {fundingPeriodId} Provider Funding Lines Current State {utcNow}.csv",
                FundingLineCsvGeneratorJobType.Released => $"{fundingStreamId} {fundingPeriodId} Provider Funding Lines Released Only {utcNow}.csv",
                FundingLineCsvGeneratorJobType.History => $"{fundingStreamId} {fundingPeriodId} Provider Funding Lines All Versions {utcNow}.csv",
                FundingLineCsvGeneratorJobType.HistoryProfileValues => $"{fundingStreamId} {fundingPeriodId} {fundingLineCode} Profile All Versions {utcNow}.csv",
                FundingLineCsvGeneratorJobType.CurrentProfileValues => $"{fundingStreamId} {fundingPeriodId} {fundingLineCode} Profile Current State {utcNow}.csv",
                FundingLineCsvGeneratorJobType.CurrentOrganisationGroupValues => $"{fundingStreamId} {fundingPeriodId} Funding Lines Current State {utcNow}.csv",
                FundingLineCsvGeneratorJobType.HistoryOrganisationGroupValues => $"{fundingStreamId} {fundingPeriodId} Funding Lines All Versions {utcNow}.csv",
                FundingLineCsvGeneratorJobType.PublishedGroups => $"{fundingStreamId} {fundingPeriodId} Published Groups {utcNow}.csv",
                FundingLineCsvGeneratorJobType.ChannelLevelPublishedGroup => $"{fundingStreamId} {fundingPeriodId} {channelCode} Published Groups {utcNow}.csv",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
