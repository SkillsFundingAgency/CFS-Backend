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

        Task<PublishedProviderVersion> GetPublishedProviderVersion(string fundingStreamId,
                string fundingPeriodId,
                string providerId,
                string version);

        Task<HttpStatusCode> UpsertPublishedFunding(PublishedFunding publishedFunding);

        Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIdsForApproval(string fundingStreamId, string fundingPeriodId);

        Task<IEnumerable<dynamic>> GetFundings(string publishedProviderVersion);

        Task<IEnumerable<PublishedProviderVersion>> GetPublishedProviderVersions(string specificationId,
            string providerId);

        Task<PublishedProvider> GetPublishedProviderById(string cosmosId, string partitionKey);

        Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIds(string fundingStreamId, string fundingPeriodId);

        Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedFundingIds(string fundingStreamId, string fundingPeriodId);

        Task<PublishedFunding> GetPublishedFundingById(string cosmosId, string partitionKey);

        Task AllPublishedProviderBatchProcessing(Func<List<PublishedProvider>, Task> persistIndexBatch, int batchSize);

        Task<IEnumerable<PublishedProviderFundingStreamStatus>> GetPublishedProviderStatusCounts(string specificationId, string providerType, string localAuthority, string status);

        Task DeleteAllPublishedProvidersByFundingStreamAndPeriod(string fundingStreamId, 
            string fundingPeriodId);

        Task DeleteAllPublishedProviderVersionsByFundingStreamAndPeriod(string fundingStreamId, 
            string fundingPeriodId);

        Task<PublishedProviderVersion> GetLatestPublishedProviderVersion(string fundingStreamId,
            string fundingPeriodId,
            string providerId);

        Task PublishedProviderBatchProcessing(string predicate,
            string specificationId,
            Func<List<PublishedProvider>, Task> batchProcessor,
            int batchSize);

        Task PublishedProviderVersionBatchProcessing(string specificationId,
            Func<List<PublishedProviderVersion>, Task> batchProcessor,
            int batchSize);
    }
}
