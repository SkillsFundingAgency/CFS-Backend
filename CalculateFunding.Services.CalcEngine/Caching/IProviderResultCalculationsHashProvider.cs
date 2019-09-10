using System.Threading.Tasks;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.CalcEngine.Caching
{
    public interface IProviderResultCalculationsHashProvider
    {
        Task<bool> TryUpdateCalculationResultHash(ProviderResult providerResult,
            int partitionIndex,
            int partitionSize);
    }
}