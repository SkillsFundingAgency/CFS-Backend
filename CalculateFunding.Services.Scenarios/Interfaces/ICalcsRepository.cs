using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface ICalcsRepository
    {
        Task<IEnumerable<CalculationCurrentVersion>> GetCurrentCalculationsBySpecificationId(string specificationId);
    }
}
