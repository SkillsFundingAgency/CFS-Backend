using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class TemplateUpdatedVariationStrategy : VariationStrategy, IVariationStrategy
    {
        public override string Name => "TemplateUpdated";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            PublishedProviderVersion priorState = providerVariationContext.PriorState;
            PublishedProviderVersion updatedState = providerVariationContext.ReleasedState;

            if (priorState == null ||
                updatedState == null ||
                priorState.TemplateVersion == updatedState.TemplateVersion)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        protected override Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            providerVariationContext.AddVariationReasons(VariationReason.TemplateUpdated);
            providerVariationContext.QueueVariationChange(new MetaDataVariationsChange(providerVariationContext, Name));

            return Task.FromResult(false);
        }
    }
}