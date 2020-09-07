using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class JobsRunning : IJobsRunning
    {
        private readonly IJobManagement _jobManagement;

        public JobsRunning(
            IJobManagement jobManagement)
        {
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));

            _jobManagement = jobManagement;
        }

        public async Task<IEnumerable<string>> GetJobTypes(string specificationId, IEnumerable<string> jobTypes)
        {
            Guard.ArgumentNotNull(jobTypes, nameof(jobTypes));

            IEnumerable<JobSummary> jobSummaries = await _jobManagement.GetLatestJobsForSpecification(specificationId, jobTypes);

            return jobSummaries.Where(_ =>  _ != null && _.RunningStatus == RunningStatus.InProgress).Select(_ => _.JobType);
        }
    }
}
