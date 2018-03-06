using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Calculator.Interfaces
{
    public interface ICalculationEngine
    {
        IEnumerable<ProviderResult> GenerateAllocations(BuildProject buildProject, IEnumerable<ProviderSummary> providers);
    }
}
