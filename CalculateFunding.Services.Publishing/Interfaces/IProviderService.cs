using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IProviderService
    {
        Task<IEnumerable<Provider>> GetProvidersByProviderVersionsId(string providerVersionId);
        Task<IDictionary<string, Provider>> GetScopedProvidersForSpecification(string specificationId, string providerVersionId);
        Task<IEnumerable<string>> GetScopedProviderIdsForSpecification(string specificationId);
        Task<(IDictionary<string, PublishedProvider> PublishedProvidersForFundingStream, IDictionary<string, PublishedProvider> ScopedPublishedProviders)> 
            GetPublishedProviders(Reference fundingStream, SpecificationSummary specification);
        Task<IDictionary<string, PublishedProvider>> GenerateMissingPublishedProviders(IEnumerable<Provider> scopedProviders,
                SpecificationSummary specification,
                Reference fundingStream,
                IDictionary<string, PublishedProvider> publishedProviders);
        Task<PublishedProvider> CreateMissingPublishedProviderForPredecessor(PublishedProvider predecessor,
            string successorId, string providerVersionId);
    }
}