using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class ClosureWithSuccessorVariationStrategy : IVariationStrategy
    {
        public string Name => nameof(ClosureWithSuccessorVariationStrategy);

        public Task DetermineVariations(ProviderVariationContext providerVariationContext)
        {
            return Task.CompletedTask;
        }
    }
}
