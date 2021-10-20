using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class FundingUpdatedVariationStrategy : VariationStrategy, IVariationStrategy
    {
        public override string Name => "FundingUpdated";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProviderVersion priorState = providerVariationContext.PriorState;
            
            if (priorState == null ||
                priorState.Provider.Status == Closed || 
                providerVariationContext.UpdatedProvider.Status == Closed ||
                providerVariationContext.UpdatedTotalFunding == priorState.TotalFunding)
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        protected override Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            providerVariationContext.AddVariationReasons(VariationReason.FundingUpdated);

            providerVariationContext.QueueVariationChange(new MetaDataVariationsChange(providerVariationContext));

            return Task.FromResult(false);
        }
    }
}