using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.ProviderLegacy;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface IAllocationModel
    {
        CalculationResultContainer Execute(IDictionary<string, ProviderSourceDataset> datasets, ProviderSummary providerSummary,
                                           IEnumerable<CalculationAggregation> aggregationValues = null);
    }
}
