using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.ProviderLegacy;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface ICalculationEngine
    {
        IAllocationModel GenerateAllocationModel(Assembly assembly);

        ProviderResult CalculateProviderResults(IAllocationModel model, string specificationId, IEnumerable<CalculationSummaryModel> calculations,
            ProviderSummary provider, IDictionary<string, ProviderSourceDataset> providerSourceDatasets, IEnumerable<CalculationAggregation> aggregations = null);
    }
}
