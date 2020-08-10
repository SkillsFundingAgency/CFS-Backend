using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.ProviderLegacy;
using FundingLine = CalculateFunding.Generators.Funding.Models.FundingLine;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface ICalculationEngine
    {
        IAllocationModel GenerateAllocationModel(Assembly assembly);

        ProviderResult CalculateProviderResults(IAllocationModel model, string specificationId, IEnumerable<CalculationSummaryModel> calculations, 
            ProviderSummary provider, IEnumerable<ProviderSourceDataset> providerSourceDatasets, IEnumerable<CalculationAggregation> aggregations = null);
    }
}
