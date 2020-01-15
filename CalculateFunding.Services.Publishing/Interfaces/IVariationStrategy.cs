using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IVariationStrategy
    {
        string Name { get; }
        
        Task DetermineVariations(ProviderVariationContext providerVariationContext);
    }
}