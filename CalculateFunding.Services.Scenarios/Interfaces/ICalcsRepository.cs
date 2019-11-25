using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs.Models;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface ICalcsRepository
    {
        Task<IEnumerable<Calculation>> GetCurrentCalculationsBySpecificationId(string specificationId);
    }
}
