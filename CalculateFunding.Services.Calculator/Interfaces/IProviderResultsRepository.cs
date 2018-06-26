using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calculator.Interfaces
{
    public interface IProviderResultsRepository
    {
        Task SaveProviderResults(IEnumerable<ProviderResult> providerResults, int degreeOfParallelism = 5);
    }
}
