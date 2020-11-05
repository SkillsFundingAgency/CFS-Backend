using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IVariationService
    {
        int ErrorCount { get; }
        string SnapShot(IDictionary<string, PublishedProvider> publishedProviders,
            string snapshotId = null);
        Task<IDictionary<string, PublishedProvider>> PrepareVariedProviders(decimal? updatedTotalFunding,
            IDictionary<string, PublishedProvider> allPublishedProviderRefreshStates,
            PublishedProvider existingPublishedProvider,
            Provider updatedProvider,
            IEnumerable<FundingVariation> variations,
            IEnumerable<ProfileVariationPointer> variationPointers,
            string snapshotId,
            string specificationProviderVersionId);
        
        Task<bool> ApplyVariations(IDictionary<string, PublishedProvider> publishedProvidersToUpdate, 
            IDictionary<string, PublishedProvider> newProviders, 
            string specificationId);
        void ClearSnapshots();
    }
}
