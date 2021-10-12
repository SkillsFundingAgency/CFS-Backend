using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class IndicativeToLiveVariationStrategy : Variation, IVariationStrategy
    {
        public string Name => "IndicativeToLive";

        public Task<bool> DetermineVariations(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProviderVersion priorState = providerVariationContext?.PriorState;
            PublishedProviderVersion refreshState = providerVariationContext?.RefreshState;

            if (priorState == null ||
                !priorState.IsIndicative ||
                refreshState == null ||
                refreshState.IsIndicative
                )
            {
                return Task.FromResult(false);
            }

            providerVariationContext.AddVariationReasons(VariationReason.IndicativeToLive);
            providerVariationContext.QueueVariationChange(new MetaDataVariationsChange(providerVariationContext));

            return Task.FromResult(false);
        }
    }
}
