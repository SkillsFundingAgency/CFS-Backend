using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Results;
using System.Collections.Generic;

namespace CalculateFunding.Services.Calculator.Interfaces
{
    public interface IAllocationModel
    {
        IEnumerable<CalculationResult> Execute(List<ProviderSourceDataset> datasets, ProviderSummary providerSummary, IEnumerable<CalculationAggregation> aggregationValues = null, IEnumerable<string> calcsToProcess = null);
    }
}
