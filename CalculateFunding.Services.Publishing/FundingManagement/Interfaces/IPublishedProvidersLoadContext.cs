using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public interface IPublishedProvidersLoadContext : IReadOnlyDictionary<string, PublishedProvider>
    {
        void AddProviders(IEnumerable<PublishedProvider> publishedProviders);
        void SetSpecDetails(string fundingStreamId, string fundingPeriodId);
        Task LoadProviders(IEnumerable<string> providerIds);
        Task<PublishedProvider> LoadProvider(string providerId);
        Task<IEnumerable<PublishedProvider>> GetOrLoadProviders(IEnumerable<string> providerIds);
    }
}