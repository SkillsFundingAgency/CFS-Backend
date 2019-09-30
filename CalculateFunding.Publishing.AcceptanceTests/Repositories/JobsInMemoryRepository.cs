using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class JobsInMemoryRepository : IJobsApiClient
    {
        Dictionary<string, Job> _jobs = new Dictionary<string, Job>();
        Dictionary<string, List<JobLog>> _jobLogs = new Dictionary<string, List<JobLog>>();

        public Task<ApiResponse<JobLog>> AddJobLog(string jobId, JobLogUpdateModel jobLogUpdateModel)
        {
            if (!_jobs.ContainsKey(jobId))
            {
                throw new Exception("Job not found");
            }

            if (!_jobLogs.ContainsKey(jobId))
            {
                _jobLogs.Add(jobId, new List<JobLog>());
            }

            Job job = _jobs[jobId];

            JobLog jobLog = new JobLog()
            {
                CompletedSuccessfully = jobLogUpdateModel.CompletedSuccessfully,
                Id = Guid.NewGuid().ToString(),
                ItemsFailed = jobLogUpdateModel.ItemsFailed,
                ItemsProcessed = jobLogUpdateModel.ItemsProcessed,
                ItemsSucceeded = jobLogUpdateModel.ItemsSucceeded,
                JobId = jobId,
                Outcome = jobLogUpdateModel.Outcome,
                Timestamp = DateTime.UtcNow,
            };

            if (job.RunningStatus == RunningStatus.Queued)
            {
                job.RunningStatus = RunningStatus.InProgress;
            }

            _jobLogs[jobId].Add(jobLog);

            return Task.FromResult(new ApiResponse<JobLog>(System.Net.HttpStatusCode.OK, jobLog));
        }

        public Task<Job> CreateJob(JobCreateModel jobCreateModel)
        {
            Guard.ArgumentNotNull(jobCreateModel, nameof(jobCreateModel));

            Job job = new Job()
            {
                Id = Guid.NewGuid().ToString(),
                Completed = null,
                CompletionStatus = null,
                CorrelationId = jobCreateModel.CorrelationId,
                Created = DateTime.Now,
                InvokerUserDisplayName = jobCreateModel.InvokerUserDisplayName,
                InvokerUserId = jobCreateModel.InvokerUserId,
                ItemCount = jobCreateModel.ItemCount,
                JobDefinitionId = jobCreateModel.JobDefinitionId,
                LastUpdated = DateTime.Now,
                MessageBody = jobCreateModel.MessageBody,
                Outcome = null,
                ParentJobId = jobCreateModel.ParentJobId,
                Properties = jobCreateModel.Properties,
                RunningStatus = RunningStatus.Queued,
                SpecificationId = jobCreateModel.SpecificationId,
                SupersededByJobId = null,
                Trigger = null,
            };

            if (job.Properties == null)
            {
                job.Properties = new Dictionary<string, string>();
            }

            if (!string.IsNullOrWhiteSpace(job.InvokerUserId))
            {
                job.Properties["user-id"] = job.InvokerUserId;
            }

            if (!string.IsNullOrWhiteSpace(job.InvokerUserDisplayName))
            {
                job.Properties["user-name"] = job.InvokerUserDisplayName;
            }

            if (!string.IsNullOrWhiteSpace(job.SpecificationId))
            {
                job.Properties["specificationId"] = job.SpecificationId;
            }

            if (!string.IsNullOrWhiteSpace(job.Id))
            {
                job.Properties["jobId"] = job.Id;
            }

            _jobs.Add(job.Id, job);

            return Task.FromResult(job);
        }

        public Task<IEnumerable<Job>> CreateJobs(IEnumerable<JobCreateModel> jobCreateModels)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<JobViewModel>> GetJobById(string jobId)
        {
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            Job job = null;
            ApiResponse<JobViewModel> result = null;
            if (_jobs.TryGetValue(jobId, out job))
            {
                JobViewModel jobViewModel = new JobViewModel()
                {
                    Id = job.Id,
                    Completed = job.Completed,
                    CompletionStatus = job.CompletionStatus,
                    CorrelationId = job.CorrelationId,
                    Created = job.Created,
                    InvokerUserDisplayName = job.InvokerUserDisplayName,
                    InvokerUserId = job.InvokerUserId,
                    ItemCount = job.ItemCount,
                    JobDefinitionId = job.JobDefinitionId,
                    MessageBody = job.MessageBody,
                    Outcome = job.Outcome,
                    ParentJobId = job.ParentJobId,
                    Properties = job.Properties,
                    RunningStatus = job.RunningStatus,
                    SpecificationId = job.SpecificationId,
                    SupersededByJobId = job.SupersededByJobId,
                    Trigger = job.Trigger,
                };

                result = new ApiResponse<JobViewModel>(System.Net.HttpStatusCode.OK, jobViewModel);
            }
            else
            {
                result = new ApiResponse<JobViewModel>(System.Net.HttpStatusCode.NotFound);
            }

            return Task.FromResult(result);
        }

        public Task<ApiResponse<IEnumerable<JobDefinition>>> GetJobDefinitions()
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<JobSummary>> GetLatestJobForSpecification(string specificationId, IEnumerable<string> jobTypes)
        {
            throw new NotImplementedException();
        }

        public Task<ApiResponse<IEnumerable<JobSummary>>> GetNonCompletedJobsWithinTimeFrame(DateTimeOffset dateTimeFrom, DateTimeOffset dateTimeTo)
        {
            throw new NotImplementedException();
        }
    }
}
