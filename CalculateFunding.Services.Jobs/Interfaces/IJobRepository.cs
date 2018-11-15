using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobRepository
    {
        Task<Job> CreateJob(Job job);

        Task<JobLog> CreateJobLog(JobLog jobLog);

        Task<Job> UpdateJob(string jobId, Job job);

        Task<JobDefinition> GetJobDefinition(string jobType);

        Task<IEnumerable<Job>> GetRunningJobsForSpecificationAndType(string specificationId, string jobType);
    }
}
