using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using System.Collections.Generic;
using System.Dynamic;
using System.Threading.Tasks;
using System.Linq;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Reporting.PublishedProviderEstate
{
    public class PublishedProviderEstateCsvGenerator : BasePublishingCsvGenerator
    {
        public const int BatchSize = 1000;

        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly Policy _publishedFundingRepositoryPolicy;

        protected override string JobDefinitionName => JobConstants.DefinitionNames.GeneratePublishedProviderEstateCsvJob;

        public PublishedProviderEstateCsvGenerator(
            IJobTracker jobTracker,
            IFileSystemAccess fileSystemAccess,
            IFileSystemCacheSettings fileSystemCacheSettings,
            IBlobClient blobClient,
            IPublishedFundingRepository publishedFundingRepository,
            ICsvUtils csvUtils,
            ILogger logger,
            IPublishedProviderCsvTransformServiceLocator publishedProviderCsvTransformServiceLocator,
            IPublishingResiliencePolicies policies)
            : base(jobTracker, fileSystemAccess, blobClient, policies, csvUtils, logger, fileSystemCacheSettings, publishedProviderCsvTransformServiceLocator)
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

            IGrouping<string, PublishedProviderVersion> lastGroupInBatch = null;

            await _publishedFundingRepositoryPolicy.ExecuteAsync(() => _publishedFundingRepository.RefreshedProviderVersionBatchProcessing(specificationId,
                publishedProviderVersions =>
                {
                    if (lastGroupInBatch != null)
                    {
                        publishedProviderVersions.AddRange(lastGroupInBatch.AsEnumerable());
                    }

                    List<IGrouping<string, PublishedProviderVersion>> providerVersionGroups = publishedProviderVersions.GroupBy(v => v.ProviderId).ToList();

                    lastGroupInBatch = providerVersionGroups.Last();
                    providerVersionGroups.Remove(lastGroupInBatch);

                    GenerateGroupedPublishedProviderEstateCsv(providerVersionGroups, publishedProviderCsvTransform, temporaryFilePath, outputHeaders);

                    outputHeaders = false;
                    processedResults = true;
                    return Task.CompletedTask;
                }, BatchSize)
            );

            if (lastGroupInBatch != null)
            {
                GenerateGroupedPublishedProviderEstateCsv(new[] { lastGroupInBatch }, publishedProviderCsvTransform, temporaryFilePath, outputHeaders);
            }

            return processedResults;
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
            return $"published-provider-estate-{specificationId}.csv";
        }
    }
}
