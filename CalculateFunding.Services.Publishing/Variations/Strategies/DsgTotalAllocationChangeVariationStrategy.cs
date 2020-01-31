using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class DsgTotalAllocationChangeVariationStrategy : Variation, IVariationStrategy
    {
        public string Name => "DsgTotalAllocationChange";
        
        public Task DetermineVariations(ProviderVariationContext providerVariationContext)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProviderVersion priorState = providerVariationContext.PriorState;
            
            if (priorState.Provider.Status == Closed || 
                providerVariationContext.UpdatedProvider.Status == Closed ||
                providerVariationContext.GeneratedProvider.TotalFunding == priorState.TotalFunding)
            {
                return Task.CompletedTask;
            }

            providerVariationContext.QueueVariationChange(new AdjustDsgProfilesForUnderOverPaymentChange(providerVariationContext));
            providerVariationContext.QueueVariationChange(new ReAdjustFundingValuesForProfileValuesChange(providerVariationContext));
            
            return Task.CompletedTask;
        }
    }
}