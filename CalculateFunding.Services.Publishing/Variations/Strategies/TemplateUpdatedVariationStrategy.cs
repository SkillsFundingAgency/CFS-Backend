using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class TemplateUpdatedVariationStrategy : IVariationStrategy
    {
        public string Name => "TemplateUpdated";

        public Task DetermineVariations(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProviderVersion priorState = providerVariationContext.PriorState;
            PublishedProviderVersion updatedState = providerVariationContext.ReleasedState;

            if (priorState == null ||
                updatedState == null ||
                priorState.TemplateVersion == updatedState.TemplateVersion)
            {
                return Task.CompletedTask;
            }

            providerVariationContext.AddVariationReasons(VariationReason.TemplateUpdated);
            providerVariationContext.QueueVariationChange(new MetaDataVariationsChange(providerVariationContext));

            return Task.CompletedTask;
        }
    }
}