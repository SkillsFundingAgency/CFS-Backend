using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobRepository
    {
        Task<Job> CreateJob(Job job);

        Task<HttpStatusCode> CreateJobLog(JobLog jobLog);

        Task<HttpStatusCode> UpdateJob(Job job);

        IEnumerable<Job> GetRunningJobsForSpecificationAndJobDefinitionId(string specificationId, string jobDefinitionId);

        Task<Job> GetJobById(string jobId);

        IEnumerable<Job> GetChildJobsForParent(string jobId);

        IEnumerable<JobLog> GetJobLogsByJobId(string jobId);
    }
}
