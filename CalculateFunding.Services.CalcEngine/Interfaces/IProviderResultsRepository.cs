using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface IProviderResultsRepository
    {
        Task<(long saveToCosmosElapsedMs, long saveToSearchElapsedMs)> SaveProviderResults(IEnumerable<ProviderResult> providerResults, int degreeOfParallelism = 5);
    }
}
