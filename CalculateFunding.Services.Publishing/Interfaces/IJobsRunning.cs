using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IJobsRunning
    {
        Task<IEnumerable<string>> GetJobTypes(string specificationId, IEnumerable<string> jobTypes);
    }
}
