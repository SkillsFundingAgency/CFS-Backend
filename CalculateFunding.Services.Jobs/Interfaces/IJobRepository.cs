using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Models.Messages;

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

        Task<IEnumerable<Job>> GetRunningJobsWithinTimeFrame(string dateTimeFrom, string dateTimeTo);

        Task DeleteJobsBySpecificationId(string specificationId, DeletionType deletionType);

        Task<Job> GetLatestJobBySpecificationIdAndDefinitionId(string specificationId, string jobDefinitionId, CompletionStatus?  completionStatusToFilter = null);

        Task<Job> GetLatestJobByTriggerEntityId(string specificationId, string entityId);
    }
}
