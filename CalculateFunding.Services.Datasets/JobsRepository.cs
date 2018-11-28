using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Datasets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets
{
    public class JobsRepository : IJobsRepository
    {
        private readonly IJobsApiClientProxy _apiClient;

        public JobsRepository(IJobsApiClientProxy apiClient)
        {
            _apiClient = apiClient;
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
    }
}
