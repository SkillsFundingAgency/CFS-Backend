using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.ProviderLegacy;
using System.Collections.Generic;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface IAllocationModel
    {
        IEnumerable<CalculationResult> Execute(List<ProviderSourceDataset> datasets, ProviderSummary providerSummary, IEnumerable<CalculationAggregation> aggregationValues = null);
    }
}
