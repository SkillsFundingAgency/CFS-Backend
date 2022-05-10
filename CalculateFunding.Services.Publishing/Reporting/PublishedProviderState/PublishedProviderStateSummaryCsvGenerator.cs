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
using System.Dynamic;
using System.Threading.Tasks;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Core;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Models;

namespace CalculateFunding.Services.Publishing.Reporting.PublishedProviderState
{
    public class PublishedProviderStateSummaryCsvGenerator : BasePublishedProviderStateCsvGenerator
    {
        public const int BatchSize = 1000;

        private readonly IPublishedProvidersSearchService _publishedProvidersSearchService;

        private readonly IProviderService _providerService;

        private readonly ISpecificationService _specificationService;

        private readonly IPoliciesApiClient _policiesApiClient;

        protected override string JobDefinitionName => JobConstants.DefinitionNames.GeneratePublishedProviderStateSummaryCsvJob;

        public PublishedProviderStateSummaryCsvGenerator(
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
            IPublishedProviderStateSummaryCsvTransformServiceLocator publishedProviderStateSummaryCsvTransformServiceLocator,
            IPublishingResiliencePolicies policies)
            : base(jobManagement, fileSystemAccess, blobClient, policies, csvUtils, logger, fileSystemCacheSettings, publishedProviderStateSummaryCsvTransformServiceLocator)
        {
            Guard.ArgumentNotNull(publishedProvidersSearchService, nameof(publishedProvidersSearchService));
            Guard.ArgumentNotNull(providerService, nameof(providerService));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));

            _publishedProvidersSearchService = publishedProvidersSearchService;
            _providerService = providerService;
            _specificationService = specificationService;
            _policiesApiClient = policiesApiClient;
        }

        protected override async Task<bool> GenerateCsv(Message message,
            string temporaryFilePath,
            IPublishedProviderStateSummaryCsvTransform publishedProviderCsvTransform)
        {
            bool outputHeaders = true;
            bool processedResults = false;

            string specificationId = message.GetUserProperty<string>("specification-id");
            string fundingPeriodId = message.GetUserProperty<string>("funding-period-id");

            SpecificationSummary specificationSummary = await _specificationService.GetSpecificationSummaryById(specificationId);
            
            if (specificationSummary == null)
            {
                throw new NonRetriableException(
                    $"Unable to generate CSV for PublishedProviderStateSummaryCsvGenerator for specification {specificationId}. Failed to get specification summary");
            }

            Reference fundingStream = specificationSummary.FundingStreams.First();

            ApiResponse<FundingConfiguration> fundingConfigurationResponse = await _policiesPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingConfiguration(fundingStream.Id, fundingPeriodId));

            if (fundingConfigurationResponse == null || !fundingConfigurationResponse.StatusCode.IsSuccess())
            {
                throw new Exception($"Failed to retrieve funding configuration for funding stream id '{ fundingStream.Id }' and period id '{ fundingPeriodId }'");
            }

            FundingConfiguration fundingConfiguration = fundingConfigurationResponse.Content;

            (IDictionary<string, PublishedProvider> publishedProvidersForFundingStream,
                    IDictionary<string, PublishedProvider> _) = await _providerService.GetPublishedProviders(specificationSummary.FundingStreams.First(), specificationSummary);

            if (publishedProvidersForFundingStream == null)
            {
                throw new NonRetriableException(
                    $"Unable to generate CSV for PublishedProviderStateSummaryCsvGenerator for specification {specificationId}. Failed to get published providers");
            }

            Dictionary<string, IEnumerable<ReleaseChannel>> releaseChannelLookupByProviderId = null;

            releaseChannelLookupByProviderId = await _publishedProvidersSearchService.GetPublishedProviderReleaseChannelsLookup(new ReleaseChannelSearch
            {
                SpecificationId = specificationId,
                FundingStreamId = specificationSummary.FundingStreams.First().Id,
                FundingPeriodId = fundingPeriodId
            });

            GeneratePublishedProviderStateSummaryCsv(fundingConfiguration, publishedProvidersForFundingStream, releaseChannelLookupByProviderId, 
                    publishedProviderCsvTransform, temporaryFilePath, outputHeaders);

            outputHeaders = false;
            processedResults = true;

            return processedResults;
        }

        protected override string GetContentDisposition(Message message)
        {
            return $"attachment; filename={GetPrettyFileName(message)}";
        }

        private void GeneratePublishedProviderStateSummaryCsv(
            FundingConfiguration fundingConfiguration,
            IDictionary<string, PublishedProvider> publishedProvidersForFundingStream,
            IDictionary<string, IEnumerable<ReleaseChannel>> releaseChannelLookupByProviderId,
            IPublishedProviderStateSummaryCsvTransform publishedProviderCsvTransform,
            string temporaryFilePath,
            bool outputHeaders)
        {
            IEnumerable<ExpandoObject> csvRows = publishedProviderCsvTransform.Transform(fundingConfiguration, publishedProvidersForFundingStream, releaseChannelLookupByProviderId);
            AppendCsvFragment(temporaryFilePath, csvRows, outputHeaders);
        }

        protected override string GetCsvFileName(Message message)
        {
            string specificationId = message.GetUserProperty<string>("specification-id");
            string fundingPeriodId = message.GetUserProperty<string>("funding-period-id");

            return $"funding-lines-{specificationId}-{FundingLineCsvGeneratorJobType.PublishedProviderStateSummary}-{fundingPeriodId}.csv";
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

            return $"{fundingStreamId} {fundingPeriodId} Provider level current state summary report {DateTimeOffset.UtcNow:s}.csv";
        }
    }
}
