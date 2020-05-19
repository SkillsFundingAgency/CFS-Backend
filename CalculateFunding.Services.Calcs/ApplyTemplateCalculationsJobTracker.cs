using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public class ApplyTemplateCalculationsJobTracker : IApplyTemplateCalculationsJobTracker
    {
        public ApplyTemplateCalculationsJobTracker(string jobId,
            IJobManagement jobs,
            ILogger logger)
        {
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(logger, nameof(logger));

            JobId = jobId;
            Jobs = jobs;
            Logger = logger;
        }

        public string JobId { get; }

        public IJobManagement Jobs { get; }

        public ILogger Logger { get; }

        public async Task<bool> TryStartTrackingJob()
        {
            try
            {
                await Jobs.RetrieveJobAndCheckCanBeProcessed(JobId);
            }
            catch(JobNotFoundException ex)
            {
                string message = $"Could not find the job with job id: '{ex.JobId}'";

                Logger.Error(message);

                throw new NonRetriableException(message);
            }
            catch (JobAlreadyCompletedException ex)
            {
                Logger.Information("ApplyTemplateCalculations job with id: '{0}' is already in a completed state with status {1}",
                    ex.Job.Id,
                    ex.Job.CompletionStatus.Value.ToString());

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
            await Jobs.AddJobLog(JobId, jobLog);
        }
    }
}