using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;
using Provider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IDetectProviderVariations
    {
        Task<ProviderVariationContext> CreateRequiredVariationChanges(PublishedProvider existingPublishedProvider,
            GeneratedProviderResult generatedProviderResult,
            Provider provider,
            IEnumerable<FundingVariation> variations,
            IDictionary<string, PublishedProvider> allPublishedProviderSnapShots,
            IDictionary<string, PublishedProvider> allPublishedProviderRefreshStates);
    }
}
