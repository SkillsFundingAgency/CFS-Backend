using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IVariationService
    {
        int ErrorCount { get; }
        Task<string> SnapShot(IDictionary<string, PublishedProvider> publishedProviders, string snapshotId = null);
        Task<IDictionary<string, PublishedProvider>> PrepareVariedProviders(decimal? updatedTotalFunding,
            IDictionary<string, PublishedProvider> allPublishedProviderRefreshStates,
            PublishedProvider existingPublishedProvider,
            Provider updatedProvider,
            IEnumerable<FundingVariation> variations,
            string snapshotId);
        Task<bool> ApplyVariations(IDictionary<string, PublishedProvider> publishedProvidersToUpdate, 
            IDictionary<string, PublishedProvider> newProviders, 
            string specificaitonId);
        void ClearSnapshots();
    }
}
