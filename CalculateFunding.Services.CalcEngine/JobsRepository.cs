using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine
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
    }
}
