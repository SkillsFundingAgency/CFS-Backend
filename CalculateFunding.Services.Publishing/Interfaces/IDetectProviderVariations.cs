using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IDetectProviderVariations
    {
        Task<ProviderVariationContext> CreateRequiredVariationChanges(PublishedProvider existingPublishedProvider, 
            GeneratedProviderResult generatedProviderResult, 
            Common.ApiClient.Providers.Models.Provider provider, 
            IEnumerable<FundingVariation> variations);
    }
}
