using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public class ApplyTemplateCalculationsJobTracker : IApplyTemplateCalculationsJobTracker
    {
        public ApplyTemplateCalculationsJobTracker(string jobId,
            IJobsApiClient jobs,
            Policy jobsResiliencePolicy,
            ILogger logger)
        {
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(jobsResiliencePolicy, nameof(jobsResiliencePolicy));
            Guard.ArgumentNotNull(logger, nameof(logger));

            JobId = jobId;
            Jobs = jobs;
            JobsResiliencePolicy = jobsResiliencePolicy;
            Logger = logger;
        }

        public string JobId { get; }

        public IJobsApiClient Jobs { get; }

        public Policy JobsResiliencePolicy { get; }

        public ILogger Logger { get; }

        public async Task<bool> TryStartTrackingJob()
        {
            ApiResponse<JobViewModel> jobResponse = await JobsResiliencePolicy.ExecuteAsync(() => Jobs.GetJobById(JobId));

            if (jobResponse?.Content == null)
            {
                string message = $"Could not find the job with job id: '{JobId}'";

                Logger.Error(message);

                throw new NonRetriableException(message);
            }

            JobViewModel applyTemplateCalculationsJob = jobResponse.Content;

            if (applyTemplateCalculationsJob.CompletionStatus.HasValue)
            {
                Logger.Information("ApplyTemplateCalculations job with id: '{0}' is already  in a completed state with status {1}",
                    applyTemplateCalculationsJob.Id,
                    applyTemplateCalculationsJob.CompletionStatus.Value.ToString());

                return false;
            }

            return true;
        }

        public async Task NotifyProgress(int itemCount)
        {
            await AddJobLog(new JobLogUpdateModel
            {
                ItemsProcessed = itemCount
            });
        }

        public async Task FailJob(string outcome)
        {
            await AddJobLog(new JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = outcome
            });
        }

        public async Task CompleteTrackingJob(string outcome, int itemCount)
        {
            await AddJobLog(new JobLogUpdateModel
            {
                ItemsSucceeded = itemCount,
                ItemsProcessed = itemCount,
                CompletedSuccessfully = true,
                Outcome = outcome
            });
        }

        private async Task AddJobLog(JobLogUpdateModel jobLog)
        {
            await JobsResiliencePolicy.ExecuteAsync(() => Jobs.AddJobLog(JobId, jobLog));
        }
    }
}