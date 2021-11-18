using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class ClosureVariationStrategy : VariationStrategy 
    {
        public override string Name => "Closure";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            PublishedProviderVersion priorState = providerVariationContext.PriorState;

            if (priorState == null ||
                priorState.Provider.Status == Closed ||
                providerVariationContext.UpdatedProvider.Status != Closed ||
                providerVariationContext.UpdatedProvider.GetSuccessors().Any())
            {
                return Task.FromResult(false);
            }

            if (providerVariationContext.UpdatedTotalFunding != priorState.TotalFunding)
            {
                providerVariationContext.RecordErrors("Unable to run Closure variation as TotalFunding has changed during the refresh funding");

                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        protected override Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            providerVariationContext.QueueVariationChange(new ZeroRemainingProfilesChange(providerVariationContext, Name));
            providerVariationContext.QueueVariationChange(new ZeroInitialPaymentProfilesChange(providerVariationContext, Name));
            providerVariationContext.QueueVariationChange(new ReAdjustFundingValuesForProfileValuesChange(providerVariationContext, Name));

            return Task.FromResult(false);
        }
    }
}
