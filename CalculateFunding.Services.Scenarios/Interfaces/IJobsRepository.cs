using CalculateFunding.Models.Jobs;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface IJobsRepository
    {
        Task<Job> CreateJob(JobCreateModel jobCreateModel);
    }
}
