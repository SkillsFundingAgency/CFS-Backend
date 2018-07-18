using CalculateFunding.Models.Results;
using System.Collections.Generic;

namespace CalculateFunding.Services.Calculator.Interfaces
{
    public interface IAllocationModel
    {
        IEnumerable<CalculationResult> Execute(List<ProviderSourceDatasetCurrent> datasets);
    }
}
