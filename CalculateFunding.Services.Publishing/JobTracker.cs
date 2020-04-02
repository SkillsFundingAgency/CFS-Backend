using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class JobTracker : IJobTracker
    {
        private readonly IJobsApiClient _jobs;
        private readonly AsyncPolicy _resiliencePolicy;
        private readonly ILogger _logger;

        public JobTracker(IJobsApiClient jobs,
            IPublishingResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(resiliencePolicies?.JobsApiClient, nameof(resiliencePolicies.JobsApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _jobs = jobs;
            _resiliencePolicy = resiliencePolicies.JobsApiClient;
            _logger = logger;
        }

        public async Task<bool> TryStartTrackingJob(string jobId, string jobType)
        {
            ApiResponse<JobViewModel> jobResponse = await _resiliencePolicy.ExecuteAsync(() => _jobs.GetJobById(jobId));

            if (jobResponse?.Content == null)
            {
                string message = $"Could not find the job with job id: '{jobId}'";

                _logger.Error(message);

                throw new NonRetriableException(message);
            }

            JobViewModel applyTemplateCalculationsJob = jobResponse.Content;

            if (applyTemplateCalculationsJob.CompletionStatus.HasValue)
            {
                _logger.Information("{0} job with id: '{1}' is already  in a completed state with status {2}",
                    jobType,
                    applyTemplateCalculationsJob.Id,
                    applyTemplateCalculationsJob.CompletionStatus.Value.ToString());

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
            await _resiliencePolicy.ExecuteAsync(() => _jobs.AddJobLog(jobId, jobLog));
        }
    }
}