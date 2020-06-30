using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
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

        Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIdsForApproval(string fundingStreamId, string fundingPeriodId, string[] providerIds = null);

        Task<IEnumerable<dynamic>> GetFundings(string publishedProviderVersion);

        Task<IEnumerable<PublishedProviderVersion>> GetPublishedProviderVersions(string specificationId,
            string providerId);

        Task<IEnumerable<PublishedProviderVersion>> GetPublishedProviderVersions(string fundingStreamId, string fundingPeriodId, string providerId, string status = null);

        Task<PublishedProvider> GetPublishedProviderById(string cosmosId, string partitionKey);

        Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIds(string fundingStreamId, string fundingPeriodId, string[] providerIds = null);

        Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedFundingIds(string fundingStreamId, string fundingPeriodId);

        Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedFundingVersionIds(string fundingStreamId, string fundingPeriodId);

        Task<PublishedFunding> GetPublishedFundingById(string cosmosId, string partitionKey);

        Task<PublishedFundingVersion> GetPublishedFundingVersionById(string cosmosId, string partitionKey);

        Task AllPublishedProviderBatchProcessing(Func<List<PublishedProvider>, Task> persistIndexBatch, int batchSize);

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

        Task PublishedProviderBatchProcessing(string predicate,
            string specificationId,
            Func<List<PublishedProvider>, Task> batchProcessor,
            int batchSize,
            string joinPredicate = null,
            string fundingLineCode = null);
        
        Task PublishedProviderVersionBatchProcessing(string predicate,
            string specificationId,
            Func<List<PublishedProviderVersion>, Task> batchProcessor,
            int batchSize,
            string joinPredicate = null,
            string fundingLineCode = null);

        Task RefreshedProviderVersionBatchProcessing(string specificationId,
            Func<List<PublishedProviderVersion>, Task> persistIndexBatch,
            int batchSize);

        Task<IEnumerable<string>> GetPublishedProviderFundingLines(string specificationId, GroupingReason fundingLineType);

        Task<IEnumerable<PublishedFundingIndex>> QueryPublishedFunding(
            IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons,
            int top,
            int? pageRef);

        Task<int> QueryPublishedFundingCount(IEnumerable<string> fundingStreamIds,
            IEnumerable<string> fundingPeriodIds,
            IEnumerable<string> groupingReasons,
            IEnumerable<string> variationReasons);
    }
}
