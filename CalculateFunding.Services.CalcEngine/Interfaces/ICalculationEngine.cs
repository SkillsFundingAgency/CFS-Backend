using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface ICalculationEngine
    {
        IAllocationModel GenerateAllocationModel(Assembly assembly);

        ProviderResult CalculateProviderResults(IAllocationModel model, BuildProject buildProject, IEnumerable<CalculationSummaryModel> calculations, 
            ProviderSummary provider, IEnumerable<ProviderSourceDataset> providerSourceDatasets, IEnumerable<CalculationAggregation> aggregations = null);
    }
}
