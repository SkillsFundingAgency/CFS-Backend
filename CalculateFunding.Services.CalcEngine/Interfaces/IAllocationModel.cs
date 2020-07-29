using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.ProviderLegacy;
using System.Collections.Generic;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface IAllocationModel
    {
        CalculationResultContainer Execute(List<ProviderSourceDataset> datasets, ProviderSummary providerSummary,
                                           IDictionary<string, Funding> fundingStreamLines,
                                           IEnumerable<CalculationAggregation> aggregationValues = null);
    }
}
