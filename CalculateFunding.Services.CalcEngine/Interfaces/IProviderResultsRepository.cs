using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface IProviderResultsRepository
    {
        Task<(long saveToCosmosElapsedMs, long saveToSearchElapsedMs, int savedProviders)> SaveProviderResults(
            IEnumerable<ProviderResult> providerResults,
            SpecificationSummary specificationSummary,
            int partitionIndex,
            int partitionSize,
            Reference user,
            string correlationId,
            string parentJobId);
    }
}
