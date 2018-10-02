using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.Calculator.Interfaces
{
    public interface ICalculationEngine
    {
        IAllocationModel GenerateAllocationModel(Assembly assembly);

        Task<IEnumerable<ProviderResult>> GenerateAllocations(BuildProject buildProject, IEnumerable<ProviderSummary> providers, Func<string, string, Task<IEnumerable<ProviderSourceDataset>>> getProviderSourceDatasets);

        ProviderResult CalculateProviderResults(IAllocationModel model, BuildProject buildProject, IEnumerable<CalculationSummaryModel> calculations, ProviderSummary provider, IEnumerable<ProviderSourceDataset> providerSourceDatasets);
    }
}
