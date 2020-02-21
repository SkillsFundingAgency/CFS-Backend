using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models;
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
            IEnumerable<FundingVariation> variations,
            IDictionary<string, PublishedProviderSnapShots> allPublishedProviderSnapShots,
            IDictionary<string, PublishedProvider> allPublishedProviderRefreshStates)
        {
            Guard.ArgumentNotNull(existingPublishedProvider, nameof(existingPublishedProvider));
            Guard.ArgumentNotNull(generatedProviderResult, nameof(generatedProviderResult));
            Guard.ArgumentNotNull(provider, nameof(provider));
            Guard.ArgumentNotNull(variations, nameof(variations));
            Guard.ArgumentNotNull(allPublishedProviderRefreshStates, nameof(allPublishedProviderRefreshStates));
            Guard.ArgumentNotNull(allPublishedProviderSnapShots, nameof(allPublishedProviderSnapShots));
            
            ProviderVariationContext providerVariationContext = new ProviderVariationContext
            {
                PublishedProvider = existingPublishedProvider,
                Result = new ProviderVariationResult(),
                UpdatedProvider = provider,
                GeneratedProvider = generatedProviderResult,
                AllPublishedProviderSnapShots = allPublishedProviderSnapShots,
                AllPublishedProvidersRefreshStates = allPublishedProviderRefreshStates
            };

            foreach (FundingVariation configuredVariation in variations.OrderBy(_ => _.Order))
            {
                IVariationStrategy variationStrategy = _variationStrategyServiceLocator.GetService(configuredVariation.Name);

                await variationStrategy.DetermineVariations(providerVariationContext, configuredVariation.FundingLineCodes);
            }

            return providerVariationContext;
        }
    }
}
