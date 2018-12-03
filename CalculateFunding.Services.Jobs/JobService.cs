using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Jobs
{
    public class JobService : IJobService, IHealthChecker
    {
        private readonly IJobRepository _jobRepository;
        private readonly IMapper _mapper;
        private readonly Polly.Policy _jobsRepositoryPolicy;
        private readonly Polly.Policy _jobsRepositoryNonAsyncPolicy;

        public JobService(IJobRepository jobRepository, IMapper mapper, IJobsResiliencePolicies resilliencePolicies)
        {
            Guard.ArgumentNotNull(jobRepository, nameof(jobRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(resilliencePolicies, nameof(resilliencePolicies));

            _jobRepository = jobRepository;
            _mapper = mapper;
            _jobsRepositoryPolicy = resilliencePolicies.JobRepository;
            _jobsRepositoryNonAsyncPolicy = resilliencePolicies.JobRepositoryNonAsync;
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

        public async Task<IActionResult> GetJobById(string jobId, bool includeChildJobs)
        {
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            Job job = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobRepository.GetJobById(jobId));

            if (job == null)
            {
                return new NotFoundResult();
            }

            JobViewModel jobViewModel = _mapper.Map<JobViewModel>(job);

            IEnumerable<Job> childJobs = _jobsRepositoryNonAsyncPolicy.Execute(() => _jobRepository.GetChildJobsForParent(jobId));

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

            IEnumerable<JobLog> logs = _jobsRepositoryNonAsyncPolicy.Execute(() => _jobRepository.GetJobLogsByJobId(jobId));
            return new OkObjectResult(logs);
        }

        public IActionResult GetJobs(string specificationId, string jobType, string entityId, RunningStatus? runningStatus, CompletionStatus? completionStatus, bool excludeChildJobs, int pageNumber)
        {
            if (pageNumber < 1)
            {
                return new BadRequestObjectResult("Invalid page number, pages start from 1");
            }

            IQueryable<Job> allJobs = _jobsRepositoryNonAsyncPolicy.Execute(() => _jobRepository.GetJobs());

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

            allJobs = allJobs.OrderByDescending(j => j.LastUpdated);

            int totalItems = allJobs.Count();

            // Limit the query to end of the requested page
            const int pageSize = 50;
            allJobs = allJobs.Take(pageNumber * pageSize);

            // Need to do actual page selection in memory as Cosmos doesn't support Skip
            IEnumerable<Job> executedJobs = allJobs.AsEnumerable();

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

        public Task<IActionResult> UpdateJob(string jobId, JobUpdateModel jobUpdate, HttpRequest request)
        {
            throw new System.NotImplementedException();
        }
    }
}
