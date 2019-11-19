using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingRepository : IHealthChecker
    {
        Task<IEnumerable<HttpStatusCode>> UpsertPublishedProviders(IEnumerable<PublishedProvider> publishedProviders);

        Task<PublishedProviderVersion> GetPublishedProviderVersion(string fundingStreamId,
                string fundingPeriodId,
                string providerId,
                string version);

        Task<HttpStatusCode> UpsertPublishedFunding(PublishedFunding publishedFunding);

        Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIdsForApproval(string fundingStreamId, string fundingPeriodId);

        Task<PublishedProvider> GetPublishedProviderById(string cosmosId, string partitionKey);

        Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIds(string fundingStreamId, string fundingPeriodId);

        Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedFundingIds(string fundingStreamId, string fundingPeriodId);

        Task<PublishedFunding> GetPublishedFundingById(string cosmosId, string partitionKey);

        Task AllPublishedProviderBatchProcessing(Func<List<PublishedProviderVersion>, Task> persistIndexBatch, int batchSize);

        Task<IEnumerable<PublishedProviderFundingStreamStatus>> GetPublishedProviderStatusCounts(string specificationId);
    }
}
