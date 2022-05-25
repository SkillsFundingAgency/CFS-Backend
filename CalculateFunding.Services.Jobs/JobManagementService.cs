using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Jobs.Interfaces;
using CalculateFunding.Services.Processing;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using OutcomeType = CalculateFunding.Models.Jobs.OutcomeType;

namespace CalculateFunding.Services.Jobs
{
    public class JobManagementService : ProcessingService, IJobManagementService, IHealthChecker
    {
        private const string SfaCorrelationId = "sfa-correlationId";

        private static readonly SemaphoreSlim SemaphoreSlim = new SemaphoreSlim(1, 1);

        private readonly IJobRepository _jobRepository;
        private readonly INotificationService _notificationService;
        private readonly IJobDefinitionsService _jobDefinitionsService;
        private readonly AsyncPolicy _jobsRepositoryPolicy;
        private readonly AsyncPolicy _cacheProviderPolicy;
        private readonly ILogger _logger;
        private readonly IValidator<CreateJobValidationModel> _createJobValidator;
        private readonly IMessengerService _messengerService;
        private readonly ICacheProvider _cacheProvider;

        public JobManagementService(
            IJobRepository jobRepository,
            INotificationService notificationService,
            IJobDefinitionsService jobDefinitionsService,
            IJobsResiliencePolicies resiliencePolicies,
            ILogger logger,
            IValidator<CreateJobValidationModel> createJobValidator,
            IMessengerService messengerService,
            ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(jobRepository, nameof(jobRepository));
            Guard.ArgumentNotNull(notificationService, nameof(notificationService));
            Guard.ArgumentNotNull(jobDefinitionsService, nameof(jobDefinitionsService));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(createJobValidator, nameof(createJobValidator));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(resiliencePolicies?.JobRepository, nameof(resiliencePolicies.JobRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.JobDefinitionsRepository, nameof(resiliencePolicies.JobDefinitionsRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.CacheProviderPolicy, nameof(resiliencePolicies.CacheProviderPolicy));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));

            _jobRepository = jobRepository;
            _notificationService = notificationService;
            _jobDefinitionsService = jobDefinitionsService;
            _jobsRepositoryPolicy = resiliencePolicies.JobRepository;
            _logger = logger;
            _createJobValidator = createJobValidator;
            _messengerService = messengerService;
            _cacheProvider = cacheProvider;
            _cacheProviderPolicy = resiliencePolicies.CacheProviderPolicy;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth jobsRepoHealth = await ((IHealthChecker)_jobRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(JobManagementService)
            };
            health.Dependencies.AddRange(jobsRepoHealth.Dependencies);
            return health;
        }

        public async Task<IActionResult> TryCreateJobs(IEnumerable<JobCreateModel> jobs, Reference user)
        {
            (bool valid, IEnumerable<JobDefinition> jobDefinitions, IActionResult validationFailureResponse) validation = await ValidateCreateJobsRequests(jobs);

            return validation.valid ? new OkObjectResult(await CreateAllJobs(jobs, user, validation.jobDefinitions, false))
                : validation.validationFailureResponse;
        }

        public async Task<IActionResult> CreateJobs(IEnumerable<JobCreateModel> jobs, Reference user)
        {
            (bool valid, IEnumerable<JobDefinition> jobDefinitions, IActionResult validationFailureResponse) validation = await ValidateCreateJobsRequests(jobs);

            if (!validation.valid)
            {
                return validation.validationFailureResponse;
            }

            IEnumerable<Job> newJobs = (await CreateAllJobs(jobs, user, validation.jobDefinitions)).Select(_ => _.Job);

            return new OkObjectResult(newJobs);
        }

        private async Task<(bool valid, IEnumerable<JobDefinition> jobDefinitions, IActionResult failureResponse)> ValidateCreateJobsRequests(IEnumerable<JobCreateModel> jobs)
        {
            Guard.ArgumentNotNull(jobs, nameof(jobs));

            if (!jobs.Any())
            {
                string message = "Empty collection of job create models was provided";

                _logger.Warning(message);

                return (false, ArraySegment<JobDefinition>.Empty, new BadRequestObjectResult(message));
            }

            IEnumerable<JobDefinition> jobDefinitions = await _jobDefinitionsService.GetAllJobDefinitions();

            if (jobDefinitions.IsNullOrEmpty())
            {
                string message = "Failed to retrieve job definitions";

                _logger.Error(message);

                return (false, ArraySegment<JobDefinition>.Empty, new InternalServerErrorResult(message));
            }

            IList<ValidationResult> validationResults = new List<ValidationResult>();

            //ensure all jobs in batch have the correct job definition
            foreach (JobCreateModel jobCreateModel in jobs)
            {
                Guard.IsNullOrWhiteSpace(jobCreateModel.JobDefinitionId, nameof(jobCreateModel.JobDefinitionId));

                JobDefinition jobDefinition = jobDefinitions?.FirstOrDefault(m => m.Id == jobCreateModel.JobDefinitionId);

                if (jobDefinition == null)
                {
                    string message = $"A job definition could not be found for id: {jobCreateModel.JobDefinitionId}";

                    _logger.Warning(message);

                    return (false, ArraySegment<JobDefinition>.Empty, new PreconditionFailedResult(message));
                }

                CreateJobValidationModel createJobValidationModel = new CreateJobValidationModel
                {
                    JobCreateModel = jobCreateModel,
                    JobDefinition = jobDefinition
                };

                ValidationResult validationResult = _createJobValidator.Validate(createJobValidationModel);
                if (validationResult != null && !validationResult.IsValid)
                {
                    validationResults.Add(validationResult);
                }
            }

            if (validationResults.Any())
            {
                return (false, ArraySegment<JobDefinition>.Empty, new BadRequestObjectResult(validationResults));
            }

            return (true, jobDefinitions, null);
        }

        private async Task<IEnumerable<JobCreateResult>> CreateAllJobs(IEnumerable<JobCreateModel> jobs,
            Reference user,
            IEnumerable<JobDefinition> jobDefinitions,
            bool throwOnCreateOrQueueErrors = true)
        {
            ICollection<JobCreateResult> createJobResults = new List<JobCreateResult>();

            foreach (JobCreateModel job in jobs)
            {
                createJobResults.Add(await TryCreateNewJob(job, user));
            }

            JobCreateResult[] successfulCreateResults = createJobResults
                .Where(_ => _.WasCreated)
                .ToArray();

            Job[] successfullyCreatedJobs = successfulCreateResults
                .Select(_ => _.Job)
                .ToArray();

            IEnumerable<JobDefinition> jobDefinitionsToSupersede = await SupersedeJobs(successfullyCreatedJobs, jobDefinitions);

            await QueueNotifications(successfulCreateResults, jobDefinitionsToSupersede);

            JobCreateErrorDetails[] errorDetails = createJobResults
                .Where(_ => _.HasError)
                .Select(_ => new JobCreateErrorDetails
                {
                    CreateRequest = _.CreateRequest,
                    Error = _.Error
                })
                .ToArray();

            if (throwOnCreateOrQueueErrors && errorDetails.Any())
            {
                throw new JobCreateException(errorDetails);
            }

            return createJobResults;
        }

        private async Task CacheJob(Job job)
        {
            // don't cache the job if there is no associated specification
            if (string.IsNullOrWhiteSpace(job.SpecificationId))
            {
                return;
            }

            JobCacheItem jobCacheItem = new JobCacheItem
            {
                Job = job
            };
            
            string cacheKey = $"{CacheKeys.LatestJobs}{job.SpecificationId}:{job.JobDefinitionId}";
            await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, jobCacheItem));

            string jobDefinitionCacheKey = $"{CacheKeys.LatestJobsByJobDefinitionIds}{job.JobDefinitionId}";
            await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync(jobDefinitionCacheKey, jobCacheItem));
        }

        private async Task UpdateJobCache(Job job)
        {
            // don't cache the job if there is no associated specification
            if (string.IsNullOrWhiteSpace(job.SpecificationId))
            {
                return;
            }

            Job latest = await _jobRepository.GetLatestJobBySpecificationIdAndDefinitionId(job.SpecificationId, job.JobDefinitionId);
            
            if (latest != null)
            {
                JobCacheItem jobCacheItem = new JobCacheItem
                {
                    Job = latest
                };

                string cacheKey = $"{CacheKeys.LatestJobs}{job.SpecificationId}:{job.JobDefinitionId}";
                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, jobCacheItem));
                string jobDefinitionCacheKey = $"{CacheKeys.LatestJobsByJobDefinitionIds}{job.JobDefinitionId}";
                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync(jobDefinitionCacheKey, jobCacheItem));

                if (latest.CompletionStatus == CompletionStatus.Succeeded)
                {
                    string latestSuccessfulJobCacheKey = $"{CacheKeys.LatestSuccessfulJobs}{job.SpecificationId}:{job.JobDefinitionId}";
                    await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync(latestSuccessfulJobCacheKey, jobCacheItem));
                }
            }
        }

        private async Task<JobCreateResult> TryCreateNewJob(JobCreateModel job, Reference user)
        {
            Guard.ArgumentNotNull(job.Trigger, nameof(job.Trigger));

            job.InvokerUserId ??= user?.Id;
            job.InvokerUserDisplayName ??= user?.Name;

            return await CreateJob(job);
        }

        private async Task QueueNotifications(IEnumerable<JobCreateResult> createdJobs, IEnumerable<JobDefinition> jobDefinitionsToSupersede)
        {
            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 30);
            foreach (JobCreateResult jobCreateResult in createdJobs)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            Job job = jobCreateResult.Job;

                            JobDefinition jobDefinition = jobDefinitionsToSupersede.First(m => m.Id == job.JobDefinitionId);

                            await QueueNewJob(job, jobDefinition, jobCreateResult.CreateRequest.Compress);

                            JobSummary jobNotification = CreateJobNotificationFromJob(job);

                            jobCreateResult.WasQueued = true;

                            await _notificationService.SendNotification(jobNotification);
                        }
                        catch (QueueJobException queueJobException)
                        {
                            jobCreateResult.Error = queueJobException.Message;
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }

            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());
        }

        private async Task<IEnumerable<JobDefinition>> SupersedeJobs(IEnumerable<Job> createdJobs, IEnumerable<JobDefinition> jobDefinitions)
        {
            IEnumerable<IGrouping<string, Job>> jobDefinitionGroups = createdJobs
                .GroupBy(j => j.JobDefinitionId);

            IEnumerable<JobDefinition> jobDefinitionsToSupersede = jobDefinitions
                .GroupBy(x => x.Id)
                .Select(x => x.First(y => y.Id == x.Key));

            foreach (IGrouping<string, Job> jobDefinitionKvp in jobDefinitionGroups)
            {
                Job jobToSupersedeOthers = jobDefinitionKvp.First();

                JobDefinition jobDefinition = jobDefinitionsToSupersede.First(m => m.Id == jobToSupersedeOthers.JobDefinitionId);

                await CheckForSupersededAndCancelOtherJobs(jobToSupersedeOthers, jobDefinition);
            }

            return jobDefinitionsToSupersede;
        }

        public async Task<IActionResult> AddJobLog(string jobId, JobLogUpdateModel jobLogUpdateModel)
        {
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));
            Guard.ArgumentNotNull(jobLogUpdateModel, nameof(jobLogUpdateModel));

            Job job = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.GetJobById(jobId));

            if (job == null)
            {
                string error = $"A job could not be found for job id: '{jobId}'";

                _logger.Error(error);

                return new NotFoundObjectResult(error);
            }

            bool needToSaveJob = false;

            if (jobLogUpdateModel.CompletedSuccessfully.HasValue)
            {
                bool completedSuccessfully = jobLogUpdateModel.CompletedSuccessfully.Value;

                job.Completed = DateTimeOffset.UtcNow;
                job.RunningStatus = RunningStatus.Completed;
                job.CompletionStatus = completedSuccessfully ? CompletionStatus.Succeeded : CompletionStatus.Failed;
                job.OutcomeType = GetCompletedJobOutcomeType(jobLogUpdateModel);
                job.Outcome = jobLogUpdateModel.Outcome;
                needToSaveJob = true;
            }
            else
            {
                if (job.RunningStatus != RunningStatus.InProgress)
                {
                    job.RunningStatus = RunningStatus.InProgress;
                    needToSaveJob = true;
                }
            }

            if (needToSaveJob)
            {
                HttpStatusCode statusCode = await UpdateJob(job);

                if (!statusCode.IsSuccess())
                {
                    string error = $"Failed to update job id: '{jobId}' with status code '{(int)statusCode}'";

                    _logger.Error(error);

                    return new InternalServerErrorResult(error);
                }
            }

            JobLog jobLog = new JobLog
            {
                Id = Guid.NewGuid().ToString(),
                JobId = jobId,
                ItemsProcessed = jobLogUpdateModel.ItemsProcessed,
                ItemsSucceeded = jobLogUpdateModel.ItemsSucceeded,
                ItemsFailed = jobLogUpdateModel.ItemsFailed,
                OutcomeType = jobLogUpdateModel.OutcomeType,
                Outcome = jobLogUpdateModel.Outcome,
                CompletedSuccessfully = jobLogUpdateModel.CompletedSuccessfully,
                Timestamp = DateTimeOffset.UtcNow
            };

            HttpStatusCode createJobLogStatus = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.CreateJobLog(jobLog));

            if (!createJobLogStatus.IsSuccess())
            {
                string error = $"Failed to create a job log for job id: '{jobId}'";

                _logger.Error(error);

                throw new Exception(error);
            }

            await SendJobLogNotification(job, jobLog);

            return new OkObjectResult(jobLog);
        }

        private static OutcomeType GetCompletedJobOutcomeType(CompletionStatus completionStatus)
        {
            return completionStatus switch
            {
                CompletionStatus.Cancelled => OutcomeType.Inconclusive,
                CompletionStatus.Superseded => OutcomeType.Inconclusive,
                CompletionStatus.TimedOut => OutcomeType.Inconclusive,
                CompletionStatus.Succeeded => OutcomeType.Succeeded,
                CompletionStatus.Failed => OutcomeType.Failed,
                _ => OutcomeType.Inconclusive
            };
        }

        private static OutcomeType GetCompletedJobOutcomeType(JobLogUpdateModel jobLogUpdateModel)
        {
            if (jobLogUpdateModel.OutcomeType.HasValue)
            {
                return jobLogUpdateModel.OutcomeType.Value;
            }

            bool? completedSuccessfully = jobLogUpdateModel.CompletedSuccessfully;

            if (completedSuccessfully.GetValueOrDefault())
            {
                return OutcomeType.Succeeded;
            }

            if (completedSuccessfully.HasValue && !completedSuccessfully.Value)
            {
                return OutcomeType.Failed;
            }

            return OutcomeType.Inconclusive;
        }

        /// <summary>
        /// Cancel job based on internal state management conditions
        /// </summary>
        /// <param name="jobId">Job ID</param>
        /// <returns></returns>
        public async Task<IActionResult> CancelJob(string jobId)
        {
            // Set running status to Cancelled and CompletionStatus to Fail

            // Send notification after status logged
            await _notificationService.SendNotification(new JobSummary());

            throw new NotImplementedException();
        }

        public async Task SupersedeJob(Job runningJob, Job replacementJob)
        {
            if (CanSupersede(runningJob, replacementJob))
            {
                runningJob.Completed = DateTimeOffset.UtcNow;
                runningJob.CompletionStatus = CompletionStatus.Superseded;
                runningJob.SupersededByJobId = replacementJob.Id;
                runningJob.RunningStatus = RunningStatus.Completed;
                runningJob.OutcomeType = OutcomeType.Inconclusive;

                HttpStatusCode statusCode = await UpdateJob(runningJob);

                if (statusCode.IsSuccess())
                {
                    JobSummary jobNotification = CreateJobNotificationFromJob(runningJob);

                    await _notificationService.SendNotification(jobNotification);
                }
                else
                {
                    _logger.Error($"Failed to update superseded job, Id: {runningJob.Id}");
                }
            }
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            // When a job completes see if the parent job can be completed
            JobSummary jobNotification = message.GetPayloadAsInstanceOf<JobSummary>();

            Guard.ArgumentNotNull(jobNotification, "message payload");

            if (jobNotification.RunningStatus == RunningStatus.Completed)
            {
                if (!message.UserProperties.ContainsKey("jobId"))
                {
                    _logger.Error("Job Notification message has no JobId");

                    return;
                }

                string jobId = message.UserProperties["jobId"].ToString();

                Job job = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.GetJobById(jobId));

                if (job == null)
                {
                    _logger.Error("Could not find job with id {JobId}", jobId);

                    return;
                }

                if (!string.IsNullOrEmpty(job.ParentJobId))
                {
                    IEnumerable<Job> childJobs = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.GetChildJobsForParent(job.ParentJobId));

                    if (CompletedAll(childJobs))
                    {
                        await SemaphoreSlim.WaitAsync();

                        try
                        {
                            Job parentJob = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.GetJobById(job.ParentJobId));

                            IEnumerable<JobDefinition> jobDefinitions = await _jobDefinitionsService.GetAllJobDefinitions();

                            JobDefinition jobDefinition = jobDefinitions.FirstOrDefault(j => j.Id == parentJob.JobDefinitionId);

                            if (parentJob.CompletionStatus == CompletionStatus.Superseded || parentJob.CompletionStatus == CompletionStatus.TimedOut)
                            {
                                return;
                            }

                            // if the parent job has pre-completion jobs and is not completing then we need to queue the pre-completion jobs
                            if (jobDefinition != null && jobDefinition.PreCompletionJobs.AnyWithNullCheck() && parentJob.RunningStatus != RunningStatus.Completing)
                            {
                                await QueuePreCompletionJobs(parentJob);
                            }
                            else
                            {
                                // the pre-completion jobs must have completed at this point as the child jobs have completed
                                await CompleteParentJob(parentJob, childJobs, job, jobId);
                            }

                        }
                        finally
                        {
                            SemaphoreSlim.Release();
                        }
                    }
                    else
                    {
                        _logger.Information("Completed Job {JobId} parent {ParentJobId} has in progress child jobs and cannot be completed", jobId, job.ParentJobId);
                    }
                }
                else
                {
                    _logger.Information("Completed Job {JobId} has no parent", jobId);
                }
            }
        }

        private async Task QueuePreCompletionJobs(Job parentJob)
        {
            IEnumerable<JobDefinition> jobDefinitions = await _jobDefinitionsService.GetAllJobDefinitions();

            JobDefinition jobDefinition = jobDefinitions.FirstOrDefault(j => j.Id == parentJob.JobDefinitionId);

            if (jobDefinition != null && jobDefinition.PreCompletionJobs.AnyWithNullCheck())
            {
                IEnumerable<JobCreateModel> jobModels = jobDefinition.PreCompletionJobs.Select(_ => new JobCreateModel
                {
                    CorrelationId = parentJob.CorrelationId,
                    InvokerUserId = parentJob.InvokerUserId,
                    InvokerUserDisplayName = parentJob.InvokerUserDisplayName,
                    JobDefinitionId = _,
                    MessageBody = parentJob.MessageBody,
                    Properties = parentJob.Properties,
                    ParentJobId = parentJob.JobId,
                    SpecificationId = parentJob.SpecificationId,
                    Trigger = parentJob.Trigger
                });

                await CreateAllJobs(jobModels, new Reference { Id = parentJob.InvokerUserId, Name = parentJob.InvokerUserDisplayName }, jobDefinitions);

                parentJob.RunningStatus = RunningStatus.Completing;

                await UpdateJob(parentJob);
            }
        }

        private static bool CompletedAll(IEnumerable<Job> jobs) =>
            jobs.IsNullOrEmpty() || jobs.Any() && jobs.All(j => j.RunningStatus == RunningStatus.Completed);


        private async Task CompleteParentJob(Job parentJob, IEnumerable<Job> childJobs, Job job, string jobId)
        {
            if (parentJob.RunningStatus != RunningStatus.Completed)
            {
                parentJob.Completed = DateTimeOffset.UtcNow;
                parentJob.RunningStatus = RunningStatus.Completed;
                parentJob.CompletionStatus = DetermineCompletionStatus(childJobs);
                parentJob.Outcome = "All child jobs completed";
                parentJob.OutcomeType = GetCompletedJobOutcomeType(parentJob.CompletionStatus.GetValueOrDefault());

                RollupChildJobOutcomes(parentJob, childJobs.Concat(new[] { job }));

                await UpdateJob(parentJob);

                _logger.Information(
                    "Parent Job {ParentJobId} of Completed Job {JobId} has been completed because all child jobs are now complete",
                    job.ParentJobId, jobId);

                await _notificationService.SendNotification(CreateJobNotificationFromJob(parentJob));
            }
        }

        private void RollupChildJobOutcomes(Job parent,
            IEnumerable<Job> children)
        {
            IEnumerable<Outcome> outcomes = children?.SelectMany(_ => _.Outcomes ?? ArraySegment<Outcome>.Empty);

            foreach (Outcome outcome in outcomes ?? ArraySegment<Outcome>.Empty)
            {
                parent.AddOutcome(outcome);
            }

            foreach (Job child in children ?? ArraySegment<Job>.Empty)
            {
                parent.AddOutcome(new Outcome
                {
                    Description = child.Outcome,
                    JobDefinitionId = child.JobDefinitionId,
                    Type = child.OutcomeType.GetValueOrDefault(),
                    IsSuccessful = child.CompletionStatus.GetValueOrDefault() == CompletionStatus.Succeeded
                });
            }
        }

        private async Task<HttpStatusCode> UpdateJob(Job job)
        {
            HttpStatusCode result = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.UpdateJob(job));

            if (result.IsSuccess())
            {

                await UpdateJobCache(job);

            }

            return result;
        }

        private async Task<Job> CreateJob(Job job)
        {
            job = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.CreateJob(job));

            if (job != null)
            {
                await CacheJob(job);
            }

            return job;
        }

        public async Task CheckAndProcessTimedOutJobs()
        {
            IEnumerable<Job> nonCompletedJobs = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.GetNonCompletedJobs());

            if (nonCompletedJobs.IsNullOrEmpty())
            {
                _logger.Information("Zero non completed jobs to process, finished processing timed out jobs");

                return;
            }

            int countOfJobsToProcess = nonCompletedJobs.Count();

            _logger.Information($"{countOfJobsToProcess} non completed jobs to process");

            IEnumerable<JobDefinition> jobDefinitions = await _jobDefinitionsService.GetAllJobDefinitions();

            if (jobDefinitions.IsNullOrEmpty())
            {
                _logger.Error("Failed to retrieve job definitions when processing timed out jobs");
                throw new Exception("Failed to retrieve job definitions when processing timed out jobs");
            }

            foreach (Job job in nonCompletedJobs)
            {
                JobDefinition jobDefinition = jobDefinitions.FirstOrDefault(m => m.Id == job.JobDefinitionId);

                if (jobDefinition == null)
                {
                    _logger.Error($"Failed to find job definition : '{job.JobDefinitionId}' for job id: '{job.Id}'");
                }
                else
                {
                    DateTimeOffset jobStartDate = job.Created;

                    TimeSpan timeout = jobDefinition.Timeout;

                    if (DateTimeOffset.UtcNow > jobStartDate.Add(timeout))
                    {
                        _logger.Information($"Job with id: '{job.Id}' as exceeded its maximum timeout threshold");

                        await TimeoutJob(job);
                    }
                }
            }

        }

        private async Task TimeoutJob(Job runningJob)
        {
            runningJob.Completed = DateTimeOffset.UtcNow;
            runningJob.CompletionStatus = CompletionStatus.TimedOut;
            runningJob.RunningStatus = RunningStatus.Completed;
            runningJob.OutcomeType = OutcomeType.Inconclusive;

            HttpStatusCode statusCode = await UpdateJob(runningJob);

            if (statusCode.IsSuccess())
            {
                JobSummary jobNotification = CreateJobNotificationFromJob(runningJob);

                await _notificationService.SendNotification(jobNotification);
            }
            else
            {
                _logger.Error($"Failed to update timeout job, Id: '{runningJob.Id}' with status code {(int)statusCode}");
            }
        }

        private CompletionStatus? DetermineCompletionStatus(IEnumerable<Job> jobs)
        {
            if (jobs.Any(j => !j.CompletionStatus.HasValue))
            {
                // There are still some jobs in progress so there is no completion status
                return null;
            }

            if (jobs.Any(j => j.CompletionStatus == CompletionStatus.TimedOut))
            {
                // At least one job timed out so that is the overall completion status for the group of jobs
                return CompletionStatus.TimedOut;
            }

            if (jobs.Any(j => j.CompletionStatus == CompletionStatus.Cancelled))
            {
                // At least one job was cancelled so that is the overall completion status for the group of jobs
                return CompletionStatus.Cancelled;
            }

            if (jobs.Any(j => j.CompletionStatus == CompletionStatus.Superseded))
            {
                // At least one job was superseded so that is the overall completion status for the group of jobs
                return CompletionStatus.Superseded;
            }

            if (jobs.Any(j => j.CompletionStatus == CompletionStatus.Failed))
            {
                // At least one job failed so that is the overall completion status for the group of jobs
                return CompletionStatus.Failed;
            }

            // Got to here so that must mean all jobs succeeded
            return CompletionStatus.Succeeded;
        }

        private JobSummary CreateJobNotificationFromJob(Job job)
        {
            return new JobSummary
            {
                CompletionStatus = job.CompletionStatus,
                InvokerUserDisplayName = job.InvokerUserDisplayName,
                InvokerUserId = job.InvokerUserId,
                ItemCount = job.ItemCount,
                JobId = job.Id,
                JobType = job.JobDefinitionId,
                ParentJobId = job.ParentJobId,
                Outcome = job.Outcome,
                RunningStatus = job.RunningStatus,
                SpecificationId = job.SpecificationId,
                StatusDateTime = DateTimeOffset.UtcNow,
                SupersededByJobId = job.SupersededByJobId,
                Trigger = job.Trigger,
                Created = job.Created,
                Outcomes = job.Outcomes,
                LastUpdated = job.LastUpdated,
                OutcomeType = job.OutcomeType
            };
        }

        private async Task CheckForSupersededAndCancelOtherJobs(Job currentJob,
            JobDefinition jobDefinition)
        {
            if (jobDefinition.SupersedeExistingRunningJobOnEnqueue)
            {
                IEnumerable<Job> runningJobs = await _jobsRepositoryPolicy.ExecuteAsync(() =>
                    _jobRepository.GetRunningJobsForSpecificationAndJobDefinitionId(currentJob.SpecificationId, jobDefinition.Id));

                if (!runningJobs.IsNullOrEmpty())
                {
                    foreach (Job runningJob in runningJobs)
                    {
                        await SupersedeJob(runningJob, currentJob);
                    }
                }
            }
        }

        private async Task<JobCreateResult> CreateJob(JobCreateModel job)
        {
            JobCreateResult createResult = new JobCreateResult
            {
                CreateRequest = job
            };

            Job newJob = new Job
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

            try
            {
                createResult.Job = await CreateJob(newJob);

                if (createResult.Job == null)
                {
                    createResult.Error = $"Failed to save new job with definition id {job.JobDefinitionId}";
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to save new job with definition id {job.JobDefinitionId}");

                createResult.Error = ex.Message;
            }

            return createResult;
        }

        private async Task SendJobLogNotification(Job job, JobLog jobLog)
        {
            JobSummary jobNotification = new JobSummary
            {
                JobId = job.Id,
                JobType = job.JobDefinitionId,
                RunningStatus = job.RunningStatus,
                CompletionStatus = job.CompletionStatus,
                SpecificationId = job.SpecificationId,
                InvokerUserDisplayName = job.InvokerUserDisplayName,
                InvokerUserId = job.InvokerUserId,
                ItemCount = job.ItemCount,
                Trigger = job.Trigger,
                ParentJobId = job.ParentJobId,
                SupersededByJobId = job.SupersededByJobId,
                Outcome = jobLog.Outcome,
                StatusDateTime = DateTimeOffset.UtcNow,
                OverallItemsFailed = jobLog.ItemsFailed,
                OverallItemsProcessed = jobLog.ItemsProcessed,
                OverallItemsSucceeded = jobLog.ItemsSucceeded,
                Created = job.Created,
                LastUpdated = job.LastUpdated,
                Outcomes = job.Outcomes,
                OutcomeType = job.OutcomeType
            };

            await _notificationService.SendNotification(jobNotification);
        }

        private async Task QueueNewJob(Job job, JobDefinition jobDefinition, bool compress = false)
        {
            string queueOrTopic = !string.IsNullOrWhiteSpace(jobDefinition.MessageBusQueue) ? jobDefinition.MessageBusQueue : jobDefinition.MessageBusTopic;

            //support parent jobs with no queue / consumers
            if (queueOrTopic.IsNullOrWhitespace()) return;

            string data = !string.IsNullOrWhiteSpace(job.MessageBody) ? job.MessageBody : null;
            IDictionary<string, string> messageProperties = job.Properties;

            string sessionId = null;

            const string jobId = "jobId";
            const string parentJobId = "parentJobId";

            if (messageProperties == null)
            {
                messageProperties = new Dictionary<string, string>
                {
                    { jobId, job.Id },
                    { SfaCorrelationId, job.CorrelationId }
                };

                if (!string.IsNullOrWhiteSpace(job.ParentJobId))
                {
                    messageProperties.Add(parentJobId, job.ParentJobId);
                }
            }
            else
            {
                if (!messageProperties.ContainsKey(jobId))
                {
                    messageProperties.Add(jobId, job.Id);
                }

                if (!string.IsNullOrWhiteSpace(job.ParentJobId) && !messageProperties.ContainsKey(parentJobId))
                {
                    messageProperties.Add(parentJobId, job.ParentJobId);
                }

                if (!messageProperties.ContainsKey(SfaCorrelationId))
                {
                    messageProperties.Add(SfaCorrelationId, job.CorrelationId);
                }

                if (!string.IsNullOrWhiteSpace(jobDefinition.SessionMessageProperty))
                {
                    //Shouldn't happen as already validated
                    if (!job.Properties.ContainsKey(jobDefinition.SessionMessageProperty))
                    {
                        string errorMessage = $"Missing session property on job with id '{job.Id}";

                        _logger.Error(errorMessage);

                        throw new Exception(errorMessage);
                    }

                    sessionId = job.Properties[jobDefinition.SessionMessageProperty];
                }
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(jobDefinition.MessageBusQueue))
                {
                    await _messengerService.SendToQueueAsJson(jobDefinition.MessageBusQueue, data, messageProperties, sessionId: sessionId, compressData: compress, processingDelay: jobDefinition.ProcessingDelay);
                }
                else
                {
                    await _messengerService.SendToTopicAsJson(jobDefinition.MessageBusTopic, data, messageProperties, compressData: compress, processingDelay: jobDefinition.ProcessingDelay);
                }
            }
            catch (Exception ex)
            {
                string message = $"Failed to queue job with id: {job.Id} on Queue/topic {queueOrTopic}. Exception type: '{ex.GetType()}'";

                _logger.Error(ex, message);

                throw new QueueJobException(message, ex);
            }
        }

        private bool CanSupersede(Job runningJob, Job replacementJob)
        {
            return string.CompareOrdinal(runningJob.Id, replacementJob.Id) != 0 && (string.IsNullOrWhiteSpace(runningJob.ParentJobId) ||
                   string.CompareOrdinal(runningJob.ParentJobId, replacementJob.ParentJobId) != 0);
        }
    }
}
