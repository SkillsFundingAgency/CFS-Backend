using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface ICalculationEngineRunningChecker
    {
        Task<bool> IsCalculationEngineRunning(string specificationId, IEnumerable<string> jobTypes);
    }
}
