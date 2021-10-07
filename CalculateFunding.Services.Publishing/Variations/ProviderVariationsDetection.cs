using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations
{
    public class ProviderVariationsDetection : IDetectProviderVariations
    {
        private readonly IVariationStrategyServiceLocator _variationStrategyServiceLocator;
        private readonly IPoliciesService _policiesService;

        public ProviderVariationsDetection(IVariationStrategyServiceLocator variationStrategyServiceLocator, IPoliciesService policiesService)
        {
            Guard.ArgumentNotNull(variationStrategyServiceLocator, nameof(variationStrategyServiceLocator));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));

            _policiesService = policiesService;
            _variationStrategyServiceLocator = variationStrategyServiceLocator;
        }

        public async Task<ProviderVariationContext> CreateRequiredVariationChanges(PublishedProvider existingPublishedProvider,
            decimal? updatedTotalFunding,
            Provider provider,
            IEnumerable<FundingVariation> variations,
            IDictionary<string, PublishedProviderSnapShots> allPublishedProviderSnapShots,
            IDictionary<string, PublishedProvider> allPublishedProviderRefreshStates,
            IEnumerable<ProfileVariationPointer> variationPointers,
            string providerVersionId,
            IDictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData)
        {
            Guard.ArgumentNotNull(existingPublishedProvider, nameof(existingPublishedProvider));
            Guard.ArgumentNotNull(updatedTotalFunding, nameof(updatedTotalFunding));
            Guard.ArgumentNotNull(provider, nameof(provider));
            Guard.ArgumentNotNull(variations, nameof(variations));
            Guard.ArgumentNotNull(allPublishedProviderRefreshStates, nameof(allPublishedProviderRefreshStates));
            Guard.ArgumentNotNull(allPublishedProviderSnapShots, nameof(allPublishedProviderSnapShots));
            
            ProviderVariationContext providerVariationContext = new ProviderVariationContext (_policiesService)
            {
                PublishedProvider = existingPublishedProvider,
                UpdatedProvider = provider,
                UpdatedTotalFunding = updatedTotalFunding,
                AllPublishedProviderSnapShots = allPublishedProviderSnapShots,
                AllPublishedProvidersRefreshStates = allPublishedProviderRefreshStates,
                ProviderVersionId = providerVersionId,
                VariationPointers = variationPointers,
                OrganisationGroupResultsData = organisationGroupResultsData
            };

            foreach (FundingVariation configuredVariation in variations.OrderBy(_ => _.Order))
            {
                IVariationStrategy variationStrategy = _variationStrategyServiceLocator.GetService(configuredVariation.Name);

                VariationStrategyResult variationStrategyResult = await variationStrategy.DetermineVariations(providerVariationContext, configuredVariation.FundingLineCodes);

                if(variationStrategyResult.StopSubsequentStrategies)
                {
                    break;
                }
            }

            return providerVariationContext;
        }
    }
}
