using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Jobs
{
    public class JobService : IJobService, IHealthChecker
    {
        private readonly IJobRepository _jobRepository;
        private readonly IMapper _mapper;
        private readonly Polly.AsyncPolicy _jobsRepositoryPolicy;
        private readonly ICacheProvider _cacheProvider;
        private readonly Polly.AsyncPolicy _cacheProviderPolicy;

        public JobService(
            IJobRepository jobRepository,
            IMapper mapper,
            IJobsResiliencePolicies resiliencePolicies,
            ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(jobRepository, nameof(jobRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies?.JobRepository, nameof(resiliencePolicies.JobRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.CacheProviderPolicy, nameof(resiliencePolicies.CacheProviderPolicy));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));

            _jobRepository = jobRepository;
            _mapper = mapper;
            _jobsRepositoryPolicy = resiliencePolicies.JobRepository;
            _cacheProviderPolicy = resiliencePolicies.CacheProviderPolicy;
            _cacheProvider = cacheProvider;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth jobsRepoHealth = await ((IHealthChecker)_jobRepository).IsHealthOk();
            (bool Ok, string Message) cacheRepoHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(JobManagementService)
            };
            health.Dependencies.AddRange(jobsRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheRepoHealth.Ok, DependencyName = cacheRepoHealth.GetType().GetFriendlyName(), Message = cacheRepoHealth.Message });

            return health;
        }

        public async Task<IActionResult> GetJobById(string jobId, bool includeChildJobs)
        {
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            Job job = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.GetJobById(jobId));

            if (job == null)
            {
                return new NotFoundResult();
            }

            JobViewModel jobViewModel = _mapper.Map<JobViewModel>(job);

            IEnumerable<Job> childJobs = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.GetChildJobsForParent(jobId));

            if (!childJobs.IsNullOrEmpty())
            {
                foreach (Job childJob in childJobs)
                {
                    jobViewModel.ChildJobs.Add(_mapper.Map<JobViewModel>(childJob));
                }
            }

            return new OkObjectResult(jobViewModel);
        }

        public async Task<IActionResult> GetJobLogs(string jobId)
        {
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            Job job = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.GetJobById(jobId));

            if (job == null)
            {
                return new NotFoundResult();
            }

            IEnumerable<JobLog> logs = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.GetJobLogsByJobId(jobId));
            return new OkObjectResult(logs);
        }

        public async Task<IActionResult> GetJobs(string specificationId, string jobType, string entityId, RunningStatus? runningStatus, CompletionStatus? completionStatus, bool excludeChildJobs, int pageNumber)
        {
            if (pageNumber < 1)
            {
                return new BadRequestObjectResult("Invalid page number, pages start from 1");
            }

            IEnumerable<Job> allJobs = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.GetJobs());

            if (!string.IsNullOrEmpty(specificationId))
            {
                allJobs = allJobs.Where(j => j.SpecificationId == specificationId);
            }

            if (!string.IsNullOrEmpty(jobType))
            {
                allJobs = allJobs.Where(j => j.JobDefinitionId == jobType);
            }

            if (!string.IsNullOrEmpty(entityId))
            {
                allJobs = allJobs.Where(j => j.Trigger.EntityId == entityId);
            }

            if (runningStatus.HasValue)
            {
                allJobs = allJobs.Where(j => j.RunningStatus == runningStatus.Value);
            }

            if (completionStatus.HasValue)
            {
                allJobs = allJobs.Where(j => j.CompletionStatus == completionStatus.Value);
            }

            if (excludeChildJobs)
            {
                allJobs = allJobs.Where(j => j.ParentJobId == null);
            }

            int totalItems = allJobs.Count();

            // Limit the query to end of the requested page
            const int pageSize = 50;
            allJobs = allJobs.Take(pageNumber * pageSize);

            // Need to do actual page selection in memory as Cosmos doesn't support Skip or ordering by date
            IEnumerable<Job> executedJobs = allJobs.AsEnumerable().OrderByDescending(j => j.LastUpdated);

            executedJobs = executedJobs.Skip((pageNumber - 1) * pageSize).Take(pageSize);
            System.Diagnostics.Debug.WriteLine("Executed Jobs: {0}", executedJobs.Count());

            IEnumerable<JobSummary> summaries = _mapper.Map<IEnumerable<JobSummary>>(executedJobs);

            JobQueryResponseModel jobQueryResponse = new JobQueryResponseModel
            {
                Results = summaries,
                CurrentPage = pageNumber,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = (int)Math.Ceiling((double)totalItems / pageSize)
            };

            return new OkObjectResult(jobQueryResponse);
        }

        public async Task<IActionResult> GetLatestJobs(string specificationId, string jobTypes)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            string[] jobDefinitionIds =
                !string.IsNullOrEmpty(jobTypes) ?
                jobTypes.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                : Array.Empty<string>();

            if (!jobDefinitionIds.Any())
            {
                return new BadRequestObjectResult("JobTypes must be provided to get latest jobs.");
            }

            ConcurrentDictionary<string, Job> jobsByDefinition = new ConcurrentDictionary<string, Job>();

            List<Task> allTasks = new List<Task>();

            foreach (string jobDefinitionId in jobDefinitionIds.Distinct())
            {
                allTasks.Add(
                Task.Run(async () =>
                {
                    string cacheKey = $"{CacheKeys.LatestJobs}{specificationId}:{jobDefinitionId}";

                    Job job = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<Job>(cacheKey));

                    if (job != null)
                    {
                        jobsByDefinition[jobDefinitionId] = job;
                    }
                    else
                    {
                        job = await _jobRepository.GetLatestJobBySpecificationIdAndDefinitionId(specificationId, jobDefinitionId);

                        // create a new empty job if no job so we don't continually make calls as the cache will be invalidated in the create job and update job
                        // with a valid job so this will only be hit if redis is empty or a job has never been created
                        job ??= new Job();

                        await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, job));

                        jobsByDefinition[jobDefinitionId] = job;
                    }
                }));
            }

            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            IEnumerable<JobSummary> jobSummaries = _mapper.Map<IEnumerable<JobSummary>>(jobsByDefinition.Values);

            JobSummary[] results = new JobSummary[jobDefinitionIds.Length];
            for (int i = 0; i < jobDefinitionIds.Length; i++)
            {
                results[i] = jobSummaries.FirstOrDefault(x => string.Equals(x.JobDefinitionId, jobDefinitionIds[i], StringComparison.InvariantCultureIgnoreCase));
            }

            return new OkObjectResult(results);
        }

        public async Task<IActionResult> GetCreatedJobsWithinTimeFrame(DateTimeOffset dateTimeFrom, DateTimeOffset dateTimeTo)
        {
            Guard.ArgumentNotNull(dateTimeFrom, nameof(dateTimeFrom));
            Guard.ArgumentNotNull(dateTimeTo, nameof(dateTimeFrom));

            if (dateTimeFrom > DateTimeOffset.UtcNow)
            {
                return new BadRequestObjectResult($"{nameof(dateTimeFrom)} cannot be in the future");
            }

            if (dateTimeTo < dateTimeFrom)
            {
                return new BadRequestObjectResult($"{nameof(dateTimeTo)} cannot be before {nameof(dateTimeFrom)}.");
            }

            string dateTimeFromAsString = dateTimeFrom.ToCosmosString();
            string dateTimeToAsString = dateTimeTo.ToCosmosString();

            IEnumerable<Job> jobs = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.GetRunningJobsWithinTimeFrame(dateTimeFromAsString, dateTimeToAsString));

            return new OkObjectResult(jobs.Select(_mapper.Map<JobSummary>));
        }

        public Task<IActionResult> UpdateJob(string jobId, JobUpdateModel jobUpdate)
        {
            throw new System.NotImplementedException();
        }
    }
}
