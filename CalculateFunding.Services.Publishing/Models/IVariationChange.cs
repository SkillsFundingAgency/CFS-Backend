using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Models
{
    public interface IVariationChange
    {
        Task Apply(IApplyProviderVariations variationsApplication);
    }
}