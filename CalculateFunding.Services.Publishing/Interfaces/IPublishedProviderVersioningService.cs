using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderVersioningService
    {
        IEnumerable<PublishedProviderCreateVersionRequest> AssemblePublishedProviderCreateVersionRequests(IEnumerable<PublishedProvider> publishedProviders, Reference author, PublishedProviderStatus publishedProviderStatus);

        Task<IEnumerable<PublishedProvider>> CreateVersions(IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests);

        Task SaveVersions(IEnumerable<PublishedProvider> publishedProviders);
        Task DeleteVersions(IEnumerable<PublishedProvider> publishedProviders);
    }
}
