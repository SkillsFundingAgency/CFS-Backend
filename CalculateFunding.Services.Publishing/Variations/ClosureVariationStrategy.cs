using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations
{
    public class ClosureVariationStrategy : IVariationStrategy
    {
        public string Name => "Closure";

        public Task DetermineVariations(ProviderVariationContext providerVariationContext)
        {
            if (providerVariationContext.PriorState.Provider.Status == "Closed" || providerVariationContext.UpdatedProvider.Status != "Closed" ||
                !string.IsNullOrWhiteSpace(providerVariationContext.UpdatedProvider.Successor))
            {
                return Task.CompletedTask;
            }
            
            if (providerVariationContext.GeneratedProvider.TotalFunding != providerVariationContext.PriorState.TotalFunding)
            {
                // throw error
            }

            // Move profiles to zero

            // Provider closed
            return Task.CompletedTask;
        }
    }
}
