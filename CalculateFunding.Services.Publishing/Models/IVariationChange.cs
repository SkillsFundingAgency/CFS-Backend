using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Models
{
    public interface IVariationChange
    {
        ProviderVariationContext VariationContext { get; }
        
        Task Apply(IApplyProviderVariations variationsApplication);
    }
}