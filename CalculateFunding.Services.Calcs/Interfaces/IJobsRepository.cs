using CalculateFunding.Models.Jobs;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IJobsRepository
    {
        Task<JobLog> AddJobLog(string jobId, JobLogUpdateModel jobLogUpdateModel);
    }
}
