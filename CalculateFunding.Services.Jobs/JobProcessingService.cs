using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Services;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.Azure.ServiceBus;
using Serilog;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Jobs
{
    public abstract class JobProcessingService : ProcessingService,  IJobProcessingService
    {
        private readonly IJobManagement _jobManagement;
        private readonly ILogger _logger;
        private const string JobIdKey = "jobId";

        public JobViewModel Job { get; private set; }

        public int? ItemsProcessed { get; set; }

        public int? ItemsSucceeded => ItemsProcessed ?? 0 - ItemsFailed ?? 0;

        public int? ItemsFailed { get; set; }

        public string Outcome { get; set; }

        public JobProcessingService(IJobManagement jobManagement,
                ILogger logger)
        {
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _jobManagement = jobManagement;
            _logger = logger;
        }

        public override async Task Run(Message message, Func<Task> func = null)
        {
            Guard.ArgumentNotNull(message, nameof(message));
            
            string jobId = message.GetUserProperty<string>(JobIdKey);

            // if the job id doesn't contain key then just execute
            if (!message.UserProperties.ContainsKey(JobIdKey))
            {
                await base.Run(message, func);

                return;
            }

            if (string.IsNullOrEmpty(jobId))
            {
                string errorMessage = "Missing job id";
                _logger.Error(errorMessage);
                throw new NonRetriableException(errorMessage);
            }

            Job = await EnsureJobCanBeProcessed(jobId);

            await StartTrackingJob(jobId);

            try
            {
                await base.Run(message, func);

                await CompleteJob(jobId);
            }
            catch (NonRetriableException ex)
            {
                await FailJob(jobId, ex.Message);
                throw;
            }
        }

        public async Task NotifyProgress(int itemCount) => await _jobManagement.AddJobLog(Job.Id, new JobLogUpdateModel { ItemsProcessed = itemCount });

        public async Task NotifyPercentComplete(int percent) => await _jobManagement.UpdateJobStatus(Job.Id, percent, null, null);

        private async Task<JobViewModel> EnsureJobCanBeProcessed(string jobId)
        {
            try
            {
                return await _jobManagement.RetrieveJobAndCheckCanBeProcessed(jobId);
            }
            catch(JobNotFoundException)
            {
                string errorMessage = $"Could not find the job with id: '{jobId}'";
                _logger.Error(errorMessage);

                throw new NonRetriableException(errorMessage);
            }
            catch (JobAlreadyCompletedException jobCompletedException)
            {
                string errorMessage = $"Received job with id: '{jobId}' is already in a completed state with status '{jobCompletedException.Job.CompletionStatus}'";
                _logger.Error(errorMessage);

                throw new NonRetriableException(errorMessage);
            }
            catch
            {
                string errorMessage = $"Job can not be run '{jobId}'";
                _logger.Error(errorMessage);

                throw new NonRetriableException(errorMessage);
            }
        }

        private async Task StartTrackingJob(string jobId)
                => await UpdateJobStatus(jobId);

        private async Task CompleteJob(string jobId)
            => await UpdateJobStatus(jobId, true);

        private async Task FailJob(string jobId, string outcome)
            => await UpdateJobStatus(jobId, false, outcome);

        private async Task UpdateJobStatus(string jobId,
            bool? completed = null,
            string outcome = null)
            => await _jobManagement.UpdateJobStatus(jobId,
                ItemsProcessed ?? 0,
                ItemsFailed ?? 0,
                completed,
                Outcome ?? outcome);
    }

}
