using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;

namespace CalculateFunding.Services.Jobs.Interfaces
{
    public interface IJobRepository
    {
        Task<Job> CreateJob(Job job);

        Task<JobLog> CreateJobLog(JobLog jobLog);

        Task<HttpStatusCode> UpdateJob(Job job);

        Task<JobDefinition> GetJobDefinition(string jobType);

        IEnumerable<Job> GetRunningJobsForSpecificationAndJobDefinitionId(string specificationId, string jobDefinitionId);
    }
}
