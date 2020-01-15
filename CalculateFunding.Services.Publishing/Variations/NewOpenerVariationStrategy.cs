using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations
{
    public class NewOpenerVariationStrategy : IVariationStrategy
    {
        public string Name => "NewOpener";

        public Task DetermineVariations(ProviderVariationContext providerVariationContext)
        {
            return Task.CompletedTask;
        }
    }
}
