using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing
{
    public class JobTracker : IJobTracker
    {
        private readonly IJobManagement _jobs;

        public JobTracker(IJobManagement jobs)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));

            _jobs = jobs;
        }

        public async Task<bool> TryStartTrackingJob(string jobId, string jobType)
        {
            try
            {
                await _jobs.RetrieveJobAndCheckCanBeProcessed(jobId);
            }
            catch(JobNotFoundException ex)
            {
                throw new NonRetriableException(ex.Message);
            }
            catch(JobAlreadyCompletedException)
            {
                return false;
            }

            await AddJobLog(new JobLogUpdateModel
            {
                ItemsFailed = 0,
                ItemsProcessed = 0,
                ItemsSucceeded = 0
            }, jobId);

            return true;
        }

        public async Task CompleteTrackingJob(string jobId)
        {
            await AddJobLog(new JobLogUpdateModel
            {
                CompletedSuccessfully = true,
                ItemsProcessed = 0,
                ItemsSucceeded = 0,
                ItemsFailed = 0
            }, jobId);
        }

        public async Task NotifyProgress(int itemCount, string jobId)
        {
            await AddJobLog(new JobLogUpdateModel
            {
                ItemsProcessed = itemCount,
            }, jobId);
        }

        public async Task FailJob(string outcome, string jobId)
        {
            await AddJobLog(new JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = outcome
            }, jobId);
        }

        private async Task AddJobLog(JobLogUpdateModel jobLog, string jobId)
        {
            await _jobs.AddJobLog(jobId, jobLog);
        }
    }
}