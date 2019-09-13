using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingRepository : IHealthChecker
    {
        Task<IEnumerable<PublishedProvider>> GetLatestPublishedProvidersBySpecification(
            string specificationId);

        Task<IEnumerable<HttpStatusCode>> UpsertPublishedProviders(IEnumerable<PublishedProvider> publishedProviders);

        Task<PublishedProviderVersion> GetPublishedProviderVersion(string fundingStreamId,
                string fundingPeriodId,
                string providerId,
                string version);

        Task<IEnumerable<PublishedProvider>> GetPublishedProvidersForApproval(
            string specificationId);

        Task<IEnumerable<PublishedFunding>> GetLatestPublishedFundingBySpecification(string specificationId);

        Task<HttpStatusCode> UpsertPublishedFunding(PublishedFunding publishedFunding);
    }
}
