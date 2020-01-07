using CalculateFunding.Models.Calcs;

using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface IProviderResultsRepository
    {
        Task<(long saveToCosmosElapsedMs, long saveToSearchElapsedMs, int savedProviders)> SaveProviderResults(
            IEnumerable<ProviderResult> providerResults, 
            int partitionIndex, 
            int partitionSize,
            int degreeOfParallelism = 5);
    }
}
