using CalculateFunding.Models.Jobs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface IJobsRepository
    {
        Task<JobLog> AddJobLog(string jobId, JobLogUpdateModel jobLogUpdateModel);

        Task<Job> CreateJob(JobCreateModel jobCreateModel);

        Task<IEnumerable<Job>> CreateJobs(IEnumerable<JobCreateModel> jobCreateModels);

        Task<JobViewModel> GetJobById(string jobId);
    }
}
