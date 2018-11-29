using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class JobsRepository : IJobsRepository
    {
        private readonly IJobsApiClientProxy _apiClient;

        public JobsRepository(IJobsApiClientProxy apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<JobLog> AddJobLog(string jobId, JobLogUpdateModel jobLogUpdateModel)
        {
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));
            Guard.ArgumentNotNull(jobLogUpdateModel, nameof(jobLogUpdateModel));

            string url = $"jobs/{jobId}/logs";

            return await _apiClient.PostAsync<JobLog, JobLogUpdateModel>(url, jobLogUpdateModel);
        }

        public async Task<Job> CreateJob(JobCreateModel jobCreateModel)
        {
            Guard.ArgumentNotNull(jobCreateModel, nameof(jobCreateModel));

            string url = $"jobs";

            IEnumerable<Job> jobs = await _apiClient.PostAsync<IEnumerable<Job>, IEnumerable<JobCreateModel>>(url, new[] { jobCreateModel });

            if (jobs.IsNullOrEmpty())
            {
                throw new Exception($"Failed to create new job of type {jobCreateModel.JobDefinitionId}");
            }

            return jobs.First();
        }

        public async Task<IEnumerable<Job>> CreateJobs(IEnumerable<JobCreateModel> jobCreateModels)
        {
            Guard.ArgumentNotNull(jobCreateModels, nameof(jobCreateModels));

            string url = $"jobs";

            IEnumerable<Job> jobs = await _apiClient.PostAsync<IEnumerable<Job>, IEnumerable<JobCreateModel>>(url, jobCreateModels);

            if (jobs.IsNullOrEmpty())
            {
                throw new Exception($"Failed to create jobs");
            }

            return jobs;
        }

        public async Task<JobViewModel> GetJobById(string jobId)
        {
            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            string url = $"jobs/{jobId}";

            return await _apiClient.GetAsync<JobViewModel>(url);
        }
    }
}
