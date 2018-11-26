using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Jobs
{
    public class JobService : IJobService
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

        public Task<IActionResult> GetJobs(string specificationId, string jobType, string entityId, RunningStatus? runningStatus, CompletionStatus? completionStatus, bool excludeChildJobs, int pageNumber, HttpRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<IActionResult> UpdateJob(string jobId, JobUpdateModel jobUpdate, HttpRequest request)
        {
            throw new System.NotImplementedException();
        }
    }
}
