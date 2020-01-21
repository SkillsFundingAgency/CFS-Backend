using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using ApiProvider = CalculateFunding.Common.ApiClient.Providers.Models.Provider;

namespace CalculateFunding.Services.Publishing.Variations
{
    public class ProviderVariationsDetection : IDetectProviderVariations
    {
        private readonly IVariationStrategyServiceLocator _variationStrategyServiceLocator;

        public ProviderVariationsDetection(IVariationStrategyServiceLocator variationStrategyServiceLocator)
        {
            Guard.ArgumentNotNull(variationStrategyServiceLocator, nameof(variationStrategyServiceLocator));

            _variationStrategyServiceLocator = variationStrategyServiceLocator;
        }

        public async Task<ProviderVariationContext> CreateRequiredVariationChanges(PublishedProvider existingPublishedProvider,
            GeneratedProviderResult generatedProviderResult,
            ApiProvider provider,
           IEnumerable<FundingVariation> variations)
        {
            ProviderVariationContext providerVariationContext = new ProviderVariationContext
            {
                PriorState = existingPublishedProvider.Current,
                ProviderId = existingPublishedProvider.Current.ProviderId,
                Result = new ProviderVariationResult(),
                UpdatedProvider = provider,
                GeneratedProvider = generatedProviderResult
            };

            foreach (string variationStrategyName in variations.OrderBy(_ => _.Order).Select(_ => _.Name))
            {
                IVariationStrategy variationStrategy = _variationStrategyServiceLocator.GetService(variationStrategyName);

                await variationStrategy.DetermineVariations(providerVariationContext);
            }

            return providerVariationContext;
        }
    }
}
