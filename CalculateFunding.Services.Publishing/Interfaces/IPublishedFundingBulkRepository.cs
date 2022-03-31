using CalculateFunding.Models.Publishing;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingBulkRepository
    {
        Task<IEnumerable<PublishedFunding>> GetPublishedFundings(
            IEnumerable<KeyValuePair<string, string>> publishedFundingIds);

        Task<IEnumerable<PublishedFundingVersion>> GetPublishedFundingVersions(
            IEnumerable<KeyValuePair<string, string>> publishedFundingVersionIds);

        Task<IEnumerable<PublishedProvider>> GetPublishedProviders(
            IEnumerable<KeyValuePair<string, string>> publishedProviderIds);

        Task<IEnumerable<PublishedProviderVersion>> GetPublishedProviderVersions(
           IEnumerable<KeyValuePair<string, string>> publishedProviderVersionIds);

        Task<IEnumerable<PublishedProvider>> TryGetPublishedProvidersByProviderId(IEnumerable<string> providerIds, string fundingStreamId, string fundingPeriodId);
        Task UpsertPublishedFundings(
            IEnumerable<PublishedFunding> publishedFundings,
            Action<Task<HttpStatusCode>, PublishedFunding> continueAction);

        Task UpsertPublishedProviders(
            IEnumerable<PublishedProvider> publishedProviders,
            Action<Task<HttpStatusCode>> continueAction);
    }
}
