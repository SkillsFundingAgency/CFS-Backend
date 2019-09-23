using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs.Interfaces
{
    [Obsolete("Replace with common nuget API client")]
    public interface ICalculationsRepository
    {
        Task<bool> IsCalculationNameValid(string specificationId, string calculationName, string existingCalculationId = null);
    }
}