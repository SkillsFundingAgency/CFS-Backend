using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedProviderVersioningService
    {
        IEnumerable<PublishedProviderCreateVersionRequest> AssemblePublishedProviderCreateVersionRequests(IEnumerable<PublishedProvider> publishedProviders, Reference author, PublishedProviderStatus publishedProviderStatus);
        Task<PublishedProvider> CreateVersion(PublishedProviderCreateVersionRequest publishedProviderCreateVersionRequest);
        Task<IEnumerable<PublishedProvider>> CreateVersions(IEnumerable<PublishedProviderCreateVersionRequest> publishedProviderCreateVersionRequests);
        Task<HttpStatusCode> SaveVersion(PublishedProviderVersion publishedProviderVersion);
        Task SaveVersions(IEnumerable<PublishedProvider> publishedProviders);
        Task DeleteVersions(IEnumerable<PublishedProvider> publishedProviders);
    }
}
