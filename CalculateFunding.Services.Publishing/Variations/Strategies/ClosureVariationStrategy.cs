using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class ClosureVariationStrategy : Variation, IVariationStrategy
    {
        public string Name => "Closure";

        public Task DetermineVariations(ProviderVariationContext providerVariationContext)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));
            
            if (providerVariationContext.PriorState.Provider.Status == Closed || 
                providerVariationContext.UpdatedProvider.Status != Closed ||
                !providerVariationContext.UpdatedProvider.Successor.IsNullOrWhitespace())
            {
                return Task.CompletedTask;
            }
            
            if (providerVariationContext.GeneratedProvider.TotalFunding != providerVariationContext.PriorState.TotalFunding)
            {
                providerVariationContext.ErrorMessages.Add("Unable to run Closure variation as TotalFunding has changed during the refresh funding");
                
                return Task.CompletedTask;
            }

            
            providerVariationContext.QueueVariationChange(new ZeroRemainingProfilesChange(providerVariationContext));
            providerVariationContext.QueueVariationChange(new ReAdjustFundingValuesForProfileValuesChange(providerVariationContext));

            return Task.CompletedTask;
        }
    }
}
