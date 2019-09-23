using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    [Obsolete("Replace with common nuget API client")]
    public interface ICalcsRepository
    {
        Task<IEnumerable<CalculationCurrentVersion>> GetCurrentCalculationsBySpecificationId(string specificationId);
    }
}
