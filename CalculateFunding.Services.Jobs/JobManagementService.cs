using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Jobs.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Serilog;

namespace CalculateFunding.Services.Jobs
{
    public class JobManagementService : IJobManagementService, IHealthChecker
    {
        private readonly IJobRepository _jobRepository;
        private readonly INotificationService _notificationService;
        private readonly IJobDefinitionsService _jobDefinitionsService;
        private readonly Polly.Policy _jobsRepositoryPolicy;
        private readonly Polly.Policy _jobsRepositoryNonAsyncPolicy;
        private readonly Polly.Policy _jobDefinitionsRepositoryPolicy;
        private readonly ILogger _logger;

        public JobManagementService(
            IJobRepository jobRepository, 
            INotificationService notificationService, 
            IJobDefinitionsService jobDefinitionsService, 
            IJobsResiliencePolicies resilliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(jobRepository, nameof(jobRepository));
            Guard.ArgumentNotNull(notificationService, nameof(notificationService));
            Guard.ArgumentNotNull(jobDefinitionsService, nameof(jobDefinitionsService));
            Guard.ArgumentNotNull(resilliencePolicies, nameof(resilliencePolicies));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _jobRepository = jobRepository;
            _notificationService = notificationService;
            _jobDefinitionsService = jobDefinitionsService;
            _jobsRepositoryPolicy = resilliencePolicies.JobRepository;
            _jobDefinitionsRepositoryPolicy = resilliencePolicies.JobDefinitionsRepository;
            _jobsRepositoryNonAsyncPolicy = resilliencePolicies.JobRepositoryNonAsync;
            _logger = logger;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth jobsRepoHealth = await ((IHealthChecker)_jobRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(JobManagementService)
            };
            health.Dependencies.AddRange(jobsRepoHealth.Dependencies);
            return health;
        }

        public async Task<IActionResult> CreateJobs(IEnumerable<JobCreateModel> jobs, HttpRequest request)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));
            Guard.ArgumentNotNull(request, nameof(request));

            if (!jobs.Any())
            {
                _logger.Warning("Empty collection of job create models was provided");

                return new BadRequestObjectResult("Empty collection of job create models was provided");
            }

            IEnumerable<JobDefinition> jobDefinitions = await _jobDefinitionsService.GetAllJobDefinitions();

            if (jobDefinitions.IsNullOrEmpty())
            {
                _logger.Error("Failed to retrieve job definitions");
                return new InternalServerErrorResult("Failed to retrieve job definitions");
            }

            //ensure all jobs in batch have the correct job definition
            foreach(JobCreateModel jobCreateModel in jobs)
            {
                Guard.IsNullOrWhiteSpace(jobCreateModel.JobDefinitionId, nameof(jobCreateModel.JobDefinitionId));

                JobDefinition jobDefinition = jobDefinitions?.FirstOrDefault(m => m.Id == jobCreateModel.JobDefinitionId);

                if (jobDefinition == null)
                {
                    _logger.Warning($"A job definition could not be found for id: {jobCreateModel.JobDefinitionId}");

                    return new PreconditionFailedResult($"A job definition could not be found for id: {jobCreateModel.JobDefinitionId}");
                }
            }

            IList<Job> createdJobs = new List<Job>();

            Reference user = request.GetUser();

            foreach (JobCreateModel job in jobs)
            {
                Guard.ArgumentNotNull(job.Trigger, nameof(job.Trigger));

                JobDefinition jobDefinition = jobDefinitions.First(m => m.Id == job.JobDefinitionId);

                if (string.IsNullOrWhiteSpace(job.InvokerUserId) || string.IsNullOrWhiteSpace(job.InvokerUserDisplayName))
                {
                    job.InvokerUserId = user.Id;
                    job.InvokerUserDisplayName = user.Name;
                }

                Job newJobResult = await CreateJob(job);

                if (newJobResult == null)
                {
                    _logger.Error($"Failed to create a job for job definition id: {job.JobDefinitionId}");
                    return new InternalServerErrorResult($"Failed to create a job for job definition id: {job.JobDefinitionId}");
                }

                createdJobs.Add(newJobResult);

                await CheckForSupersededAndCancelOtherJobs(newJobResult, jobDefinition);

                JobNotification jobNotification = new JobNotification
                {
                    JobId = newJobResult.Id,
                    JobType = jobDefinition.Id,
                    RunningStatus = RunningStatus.Queued,
                    SpecificationId = newJobResult.SpecificationId,
                    InvokerUserDisplayName = newJobResult.InvokerUserDisplayName,
                    InvokerUserId = newJobResult.InvokerUserId,
                    ItemCount = newJobResult.ItemCount,
                    Trigger = newJobResult.Trigger,
                    ParentJobId = newJobResult.ParentJobId,
                    StatusDateTime = DateTimeOffset.Now.ToLocalTime()
                };

                await _notificationService.SendNotification(jobNotification);
            }

            return new OkObjectResult(createdJobs);
        }

        /// <summary>
        /// Add job log
        /// </summary>
        /// <param name="job"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IActionResult> AddJobLog(JobLogUpdateModel job, HttpRequest request)
        {
            // This method will be responsible for saving job logs, plus performing state management based on reported status
            // Lots of logic will be triggered from this function, eg RunningStatus, CompletionStatus, setting Outcome on Job

            // A job is complete (CompletedStatus is updated to Successful or Failed) on the job 
            // when the JobLogUpdateModel.CompletedSuccessfully is set to a non null value

            await _jobRepository.CreateJobLog(new JobLog());

            // Send notification after status logged
            await _notificationService.SendNotification(new JobNotification());

            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Cancel job based on internal state management conditions
        /// </summary>
        /// <param name="jobId">Job ID</param>
        /// <returns></returns>
        public async Task CancelJob(string jobId)
        {
            // Set running status to Cancelled and CompletionStatus to Fail

            // Send notification after status logged
            await _notificationService.SendNotification(new JobNotification());

            throw new System.NotImplementedException();
        }

        /// <summary>
        /// Cancel job based on user input - may not be needed for first phase
        /// </summary>
        /// <param name="jobId">Job ID</param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<IActionResult> CancelJob(string jobId, HttpRequest request)
        {
            // Send notification after status logged
            await _notificationService.SendNotification(new JobNotification());

            throw new System.NotImplementedException();
        }

        public async Task SupersedeJob(Job runningJob, string replacementJobId)
        {
            runningJob.Completed = DateTimeOffset.Now.ToLocalTime();
            runningJob.CompletionStatus = CompletionStatus.Superseded;
            runningJob.SupersededByJobId = replacementJobId;
            runningJob.RunningStatus = RunningStatus.Completed;

            HttpStatusCode statusCode = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.UpdateJob(runningJob));

            if (statusCode.IsSuccess())
            {
                JobNotification jobNotification = new JobNotification
                {
                    JobId = runningJob.Id,
                    JobType = runningJob.JobDefinitionId,
                    RunningStatus = runningJob.RunningStatus,
                    CompletionStatus = runningJob.CompletionStatus,
                    SpecificationId = runningJob.SpecificationId,
                    InvokerUserDisplayName = runningJob.InvokerUserDisplayName,
                    InvokerUserId = runningJob.InvokerUserId,
                    ItemCount = runningJob.ItemCount,
                    Trigger = runningJob.Trigger,
                    ParentJobId = runningJob.ParentJobId,
                    SupersededByJobId = runningJob.SupersededByJobId,
                    StatusDateTime = DateTimeOffset.Now.ToLocalTime()
                };

                await _notificationService.SendNotification(jobNotification);
            }
            else
            {
                _logger.Error($"Failed to update superseded job, Id: {runningJob.Id}");
            }
        }

        public async Task TimeoutJob(string jobId)
        {
            // Send notification after status logged
            await _notificationService.SendNotification(new JobNotification());

            throw new System.NotImplementedException();
        }

        public async Task ProcessJobCompletion(Message message)
        {
            // When a job completes see if the parent job can be completed
            JobNotification jobNotification = message.GetPayloadAsInstanceOf<JobNotification>();

            if (jobNotification.RunningStatus == RunningStatus.Completed)
            {
                string jobId = message.UserProperties["jobId"].ToString();

                if (string.IsNullOrEmpty(jobId))
                {
                    _logger.Error("Job Notification message has no JobId");
                    return;
                }

                Job job = await _jobRepository.GetJobById(jobId);

                if (!string.IsNullOrEmpty(job.ParentJobId))
                {
                    IEnumerable<Job> childJobs = await _jobRepository.GetChildJobsForParent(job.ParentJobId);

                    if (childJobs.Count() > 0 && childJobs.All(j => j.RunningStatus == RunningStatus.Completed))
                    {
                        Job parentJob = await _jobRepository.GetJobById(job.ParentJobId);

                        parentJob.Completed = DateTimeOffset.Now;
                        parentJob.RunningStatus = RunningStatus.Completed;
                        parentJob.CompletionStatus = DetermineCompletionStatus(childJobs);

                        await _jobRepository.UpdateJob(parentJob.Id, parentJob);

                        _notificationService.SendNotification(CreateJobNotificationFromJob(parentJob));
                    }

                }
            }
        }

        private async Task CheckForSupersededAndCancelOtherJobs(Job currentJob, JobDefinition jobDefinition)
        {
            if (jobDefinition.SupersedeExistingRunningJobOnEnqueue)
            {
                IEnumerable<Job> runningJobs = _jobsRepositoryNonAsyncPolicy.Execute(() => _jobRepository.GetRunningJobsForSpecificationAndJobDefinitionId(currentJob.SpecificationId, jobDefinition.Id));

                if (!runningJobs.IsNullOrEmpty())
                {
                    foreach (Job runningJob in runningJobs)
                    {
                        await SupersedeJob(runningJob, currentJob.Id);
                    }
                }
            }
        }

        private async Task<Job> CreateJob(JobCreateModel job)
        {
            Job newJob = new Job()
            {
                JobDefinitionId = job.JobDefinitionId,
                InvokerUserId = job.InvokerUserId,
                InvokerUserDisplayName = job.InvokerUserDisplayName,
                ItemCount = job.ItemCount,
                SpecificationId = job.SpecificationId,
                Trigger = job.Trigger,
                ParentJobId = job.ParentJobId,
                CorrelationId = job.CorrelationId,
                Properties = job.Properties,
                MessageBody = job.MessageBody
            };

            Job newJobResult = null;

            try
            {
                newJobResult = await _jobDefinitionsRepositoryPolicy.ExecuteAsync(() => _jobRepository.CreateJob(newJob));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to save new job with definition id {job.JobDefinitionId}");
            }

            return newJobResult;
        }
    }
}
