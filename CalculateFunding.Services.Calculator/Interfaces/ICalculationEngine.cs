﻿using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calculator.Interfaces
{
    public interface ICalculationEngine
    {
        IAllocationModel GenerateAllocationModel(BuildProject buildProject);

        Task<IEnumerable<ProviderResult>> GenerateAllocations(BuildProject buildProject, IEnumerable<ProviderSummary> providers, Func<string, string, Task<IEnumerable<ProviderSourceDataset>>> getProviderSourceDatasets);

        ProviderResult CalculateProviderResults(IAllocationModel model, BuildProject buildProject, ProviderSummary provider, IEnumerable<ProviderSourceDataset> providerSourceDatasets);
    }
}
