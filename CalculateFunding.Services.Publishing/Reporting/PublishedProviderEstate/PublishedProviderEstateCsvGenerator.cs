using System;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.JobManagement;

namespace CalculateFunding.Services.Publishing.Reporting.PublishedProviderEstate
{
    public class PublishedProviderEstateCsvGenerator : BasePublishingCsvGenerator
    {
        public const int BatchSize = 1000;

        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly AsyncPolicy _publishedFundingRepositoryPolicy;

        protected override string JobDefinitionName => JobConstants.DefinitionNames.GeneratePublishedProviderEstateCsvJob;

        public PublishedProviderEstateCsvGenerator(
            IJobManagement jobManagement,
            IFileSystemAccess fileSystemAccess,
            IFileSystemCacheSettings fileSystemCacheSettings,
            IBlobClient blobClient,
            IPublishedFundingRepository publishedFundingRepository,
            ICsvUtils csvUtils,
            ILogger logger,
            IPublishedProviderCsvTransformServiceLocator publishedProviderCsvTransformServiceLocator,
            IPublishingResiliencePolicies policies)
            : base(jobManagement, fileSystemAccess, blobClient, policies, csvUtils, logger, fileSystemCacheSettings, publishedProviderCsvTransformServiceLocator)
        {
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(policies?.PublishedFundingRepository, nameof(policies.PublishedFundingRepository));

            _publishedFundingRepository = publishedFundingRepository;
            _publishedFundingRepositoryPolicy = policies.PublishedFundingRepository;
        }

        protected override async Task<bool> GenerateCsv(Message message,
            string temporaryFilePath,
            IPublishedProviderCsvTransform publishedProviderCsvTransform)
        {
            bool outputHeaders = true;
            bool processedResults = false;

            string specificationId = message.GetUserProperty<string>("specification-id");

            await _publishedFundingRepositoryPolicy.ExecuteAsync(() => _publishedFundingRepository.RefreshedProviderVersionBatchProcessing(specificationId,
                publishedProviderVersions =>
                {
                    List<IGrouping<string, PublishedProviderVersion>> providerVersionGroups = publishedProviderVersions.GroupBy(v => v.ProviderId).ToList();

                    GenerateGroupedPublishedProviderEstateCsv(providerVersionGroups, publishedProviderCsvTransform, temporaryFilePath, outputHeaders);

                    outputHeaders = false;
                    processedResults = true;
                    return Task.CompletedTask;
                }, BatchSize)
            );

            return processedResults;
        }

        protected override string GetContentDisposition(Message message)
        {
            return $"attachment; filename={GetPrettyFileName(message)}";
        }

        private void GenerateGroupedPublishedProviderEstateCsv(
            IEnumerable<IGrouping<string, PublishedProviderVersion>> providerVersionGroup,
            IPublishedProviderCsvTransform publishedProviderCsvTransform,
            string temporaryFilePath,
            bool outputHeaders)
        {
            IEnumerable<ExpandoObject> csvRows = publishedProviderCsvTransform.Transform(providerVersionGroup);
            AppendCsvFragment(temporaryFilePath, csvRows, outputHeaders);
        }

        protected override string GetCsvFileName(Message message)
        {
            string specificationId = message.GetUserProperty<string>("specification-id");
            string fundingPeriodId = message.GetUserProperty<string>("funding-period-id");

            return $"funding-lines-{specificationId}-{FundingLineCsvGeneratorJobType.HistoryPublishedProviderEstate}-{fundingPeriodId}.csv";
        }

        protected override IDictionary<string, string> GetMetadata(Message message)
        {
            return new Dictionary<string, string>
            {
                { "specification-id", message.GetUserProperty<string>("specification-id") },
                { "funding-stream-id", message.GetUserProperty<string>("funding-stream-id") },
                { "funding-period-id", message.GetUserProperty<string>("funding-period-id") },
                { "jobId", message.GetUserProperty<string>("jobId") },
                { "job-type", message.GetUserProperty<string>("job-type") },
                { "file-name", GetPrettyFileName(message) }
            };
        }

        private string GetPrettyFileName(Message message)
        {
            string fundingStreamId = message.GetUserProperty<string>("funding-stream-id");
            string fundingPeriodId = message.GetUserProperty<string>("funding-period-id");

            return $"{fundingStreamId} {fundingPeriodId} Provider Estate Variations {DateTimeOffset.UtcNow:s}";
        }
    }
}
