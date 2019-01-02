using CalculateFunding.Models.Calcs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface ICalcsRepository
    {
        Task<IEnumerable<CalculationCurrentVersion>> GetCurrentCalculationsBySpecificationId(string specificationId);
    }
}
