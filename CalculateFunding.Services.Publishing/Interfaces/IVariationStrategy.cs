using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IVariationStrategy
    {
        string Name { get; }
        
        Task<bool> DetermineVariations(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes);
    }
}