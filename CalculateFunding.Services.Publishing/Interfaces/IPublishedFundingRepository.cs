using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingRepository : IHealthChecker
    {
        Task<IEnumerable<HttpStatusCode>> UpsertPublishedProviders(IEnumerable<PublishedProvider> publishedProviders);

        Task<HttpStatusCode> UpsertPublishedProvider(PublishedProvider publishedProvider);

        Task<PublishedProviderVersion> GetPublishedProviderVersion(string fundingStreamId,
                string fundingPeriodId,
                string providerId,
                string version);

        Task<HttpStatusCode> UpsertPublishedFunding(PublishedFunding publishedFunding);

        Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIdsForApproval(string fundingStreamId, string fundingPeriodId, string[] publishedProviderIds = null);

        Task<IEnumerable<dynamic>> GetFundings(string publishedProviderVersion);

        Task<IEnumerable<PublishedProviderVersion>> GetPublishedProviderVersions(string specificationId,
            string providerId);

        Task<IEnumerable<PublishedProviderVersion>> GetPublishedProviderVersions(string fundingStreamId, string fundingPeriodId, string providerId, string status = null);

        Task<IEnumerable<PublishedProviderVersion>> GetPublishedProviderVersionsForApproval(
            string specificationId,
            string fundingStreamId,
            string providerId);

        Task<PublishedProvider> GetPublishedProviderById(string cosmosId, string partitionKey);

        Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIds(string fundingStreamId, string fundingPeriodId, string[] providerIds = null);

        Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedFundingIds(string fundingStreamId, string fundingPeriodId);

        Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedFundingVersionIds(string fundingStreamId, string fundingPeriodId);

        Task<PublishedFunding> GetPublishedFundingById(string cosmosId, string partitionKey);

        Task<PublishedFundingVersion> GetPublishedFundingVersionById(string cosmosId, string partitionKey);

        Task AllPublishedProviderBatchProcessing(Func<List<PublishedProvider>, Task> persistIndexBatch, int batchSize, string specificationId);

        Task<IEnumerable<PublishedProviderFundingStreamStatus>> GetPublishedProviderStatusCounts(string specificationId, string providerType, string localAuthority, string status);

        Task DeleteAllPublishedProvidersByFundingStreamAndPeriod(string fundingStreamId, 
            string fundingPeriodId);

        Task DeleteAllPublishedProviderVersionsByFundingStreamAndPeriod(string fundingStreamId, 
            string fundingPeriodId);

        Task DeleteAllPublishedFundingsByFundingStreamAndPeriod(string fundingStreamId,
            string fundingPeriodId);

        Task DeleteAllPublishedFundingVersionsByFundingStreamAndPeriod(string fundingStreamId,
            string fundingPeriodId);

        Task<PublishedProvider> GetPublishedProvider(string fundingStreamId,
            string fundingPeriodId,
            string providerId);

        Task<PublishedProviderVersion> GetLatestPublishedProviderVersion(string fundingStreamId,
            string fundingPeriodId,
            string providerId);

        Task<PublishedProviderVersion> GetLatestPublishedProviderVersionBySpecificationId(
            string specificationId,
            string fundingStreamId,
            string providerId);
        Task<PublishedProviderVersion> GetPublishedProviderVersionById(string publishedProviderVersionId);

        Task PublishedProviderBatchProcessing(string predicate,
            string specificationId,
            Func<List<PublishedProvider>, Task> batchProcessor,
            int batchSize,
            string joinPredicate = null,
            string fundingLineCode = null);

        Task<IEnumerable<string>> GetPublishedProviderFundingLines(string specificationId, GroupingReason fundingLineType);

        Task<IEnumerable<PublishedFundingIndex>> QueryPublishedFunding(IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons,
            int top,
            int? pageRef,
            int totalCount);

        Task<int> QueryPublishedFundingCount(IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons);

        Task<(string providerVersionId, string providerId)> GetPublishedProviderId(string publishedProviderVersion);

        Task PublishedGroupBatchProcessing(string specificationId,
            Func<List<PublishedFunding>, Task> batchProcessor,
            int batchSize);

        Task<IEnumerable<PublishedProvider>> QueryPublishedProvider(string specificationId, IEnumerable<string> fundingIds);

        Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedFundingIds(string specificationId, GroupingReason? groupReason = null);

        Task<IEnumerable<PublishedProviderFunding>> GetPublishedProvidersFunding(IEnumerable<string> publishedProviderIds,
            string specificationId,
            params PublishedProviderStatus[] statuses);

        Task<IEnumerable<string>> GetPublishedProviderErrorSummaries(string specificationId);

        Task<IEnumerable<PublishedProviderFundingCsvData>> GetPublishedProvidersFundingDataForCsvReport(IEnumerable<string> publishedProviderIds,
            string specificationId,
            params PublishedProviderStatus[] statuses);

        Task<DateTime?> GetLatestPublishedDate(string fundingStreamId,
            string fundingPeriodId);

        Task<IDictionary<string, string>> GetPublishedProviderIdsForUkprns(string fundingStreamId,
            string fundingPeriodId,
            string[] ukprns);

        Task<IEnumerable<string>> RemoveIdsInError(IEnumerable<string> publishedProviderIds);

        Task<IEnumerable<string>> GetPublishedProviderIds(string specificationId);

        Task<IEnumerable<string>> GetPublishedProviderPublishedProviderIds(string specificationId);

        Task DeletePublishedProviders(IEnumerable<PublishedProvider> publishedProviders);

        ICosmosDbFeedIterator<PublishedProviderVersion> GetPublishedProviderVersionsForBatchProcessing(string predicate,
            string specificationId,
            int batchSize,
            string joinPredicate = null,
            string fundingLineCode = null);

        ICosmosDbFeedIterator<PublishedFundingVersion> GetPublishedFundingVersionsForBatchProcessing(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            int batchSize);

        ICosmosDbFeedIterator<PublishedFunding> GetPublishedFundingForBatchProcessing(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            int batchSize);

        ICosmosDbFeedIterator<PublishedProviderVersion> GetRefreshedProviderVersionBatchProcessing(string specificationId,
            int batchSize);
    }
}
