using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Jobs
{
    public class JobManagementService : IJobManagementService
    {
        private readonly IJobRepository _jobRepository;
        private readonly INotificationService _notificationService;

        public JobManagementService(IJobRepository jobRepository, INotificationService notificationService)
        {
            _jobRepository = jobRepository;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> CreateJob(JobCreateModel job, HttpRequest request)
        {
            JobType jobType = await _jobRepository.GetJobType(job.JobType);

            // Validate job type

            // Map and setup job properties
            Job newJob = new Job()
            {
                Created = DateTimeOffset.Now,
                RunningStatus = RunningStatus.Queued,
            };

            Job newJobResult = await _jobRepository.CreateJob(newJob);
            await CheckForSupersededAndCancelOtherJobs(newJobResult, jobType);

            // Notifiy this job has started and is queued
            await _notificationService.SendNotification(new JobNotification());

            throw new System.NotImplementedException();
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

        public async Task SupersedeJob(string existingJobId, string replacementJobId)
        {
            // Set running status to Cancelled and CompletionStatus to Fail and SupersededByJobId on existing job to replacementJobId

            // Send notification after status logged
            await _notificationService.SendNotification(new JobNotification());

            throw new System.NotImplementedException();
        }

        public async Task TimeoutJob(string jobId)
        {
            // Send notification after status logged
            await _notificationService.SendNotification(new JobNotification());

            throw new System.NotImplementedException();
        }

        private async Task CheckForSupersededAndCancelOtherJobs(Job currentJob, JobType jobType)
        {
            if (jobType.SupersedeExistingRunningJobOnEnqueue)
            {
                IEnumerable<Job> runningJobs = await _jobRepository.GetRunningJobsForSpecificationAndType(currentJob.SpecificationId, jobType.JobTypeId);
                foreach (Job runningJob in runningJobs)
                {
                    await SupersedeJob(runningJob.JobId, currentJob.JobId);

                    // Set all properties
                    runningJob.Completed = DateTimeOffset.UtcNow;
                    runningJob.CompletionStatus = CompletionStatus.Fail;
                    runningJob.SupersededByJobId = currentJob.JobId;

                    await _jobRepository.UpdateJob(runningJob.JobId, runningJob);

                    // Notify this job has been superceded.
                    await _notificationService.SendNotification(new JobNotification());

                }
            }
        }
    }
}
