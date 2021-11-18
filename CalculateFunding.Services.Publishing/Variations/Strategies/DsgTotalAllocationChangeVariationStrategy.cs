using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class DsgTotalAllocationChangeVariationStrategy : VariationStrategy, IVariationStrategy
    {
        public override string Name => "DsgTotalAllocationChange";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProviderVersion priorState = providerVariationContext.PriorState;
            
            if (priorState == null ||
                priorState.Provider.Status == Closed || 
                providerVariationContext.UpdatedProvider.Status == Closed)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        protected override Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            providerVariationContext.QueueVariationChange(new AdjustDsgProfilesForUnderOverPaymentChange(providerVariationContext, Name));
            providerVariationContext.QueueVariationChange(new ReAdjustFundingValuesForProfileValuesChange(providerVariationContext, Name));

            return Task.FromResult(false);
        }
    }
}