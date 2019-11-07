using System.Collections.Generic;
using System.Linq;
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

        Task<IEnumerable<Job>> GetRunningJobsForSpecificationAndJobDefinitionId(string specificationId, string jobDefinitionId);

        Task<IEnumerable<Job>> GetJobs();

        Task<Job> GetJobById(string jobId);

        Task<IEnumerable<Job>> GetChildJobsForParent(string jobId);

        Task<IEnumerable<JobLog>> GetJobLogsByJobId(string jobId);

        Task<IEnumerable<Job>> GetNonCompletedJobs();

        Task<Job> GetLatestJobBySpecificationId(string specificationId, IEnumerable<string> jobDefinitionIds = null);

        Task<IEnumerable<Job>> GetRunningJobsWithinTimeFrame(string dateTimeFrom, string dateTimeTo);
    }
}
