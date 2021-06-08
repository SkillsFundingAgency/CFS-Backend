using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Models.Publishing;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IVariationService
    {
        int ErrorCount { get; }
        string SnapShot(IDictionary<string, PublishedProvider> publishedProviders,
            string snapshotId = null);
        Task<ProviderVariationContext> PrepareVariedProviders(decimal? updatedTotalFunding,
            IDictionary<string, PublishedProvider> allPublishedProviderRefreshStates,
            PublishedProvider existingPublishedProvider,
            Provider updatedProvider,
            IEnumerable<FundingVariation> variations,
            IEnumerable<ProfileVariationPointer> variationPointers,
            string snapshotId,
            string specificationProviderVersionId,
            IDictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData);
        
        Task<bool> ApplyVariations(IRefreshStateService refreshStateService, 
            string specificaitonId,
            string jobId);
        void ClearSnapshots();
    }
}
