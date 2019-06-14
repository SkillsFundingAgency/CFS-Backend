using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Datasets.Interfaces;

namespace CalculateFunding.Services.Datasets
{
    public class CalcsRepository : ICalcsRepository
    {
        const string buildProjectsUrl = "calcs/get-buildproject-by-specification-id?specificationId=";

        const string updateRelationshipsUrl = "calcs/update-buildproject-relationships?specificationId=";

        const string calculationsUrl = "calcs/current-calculations-for-specification?specificationId=";

        private readonly ICalcsApiClientProxy _apiClient;

        public CalcsRepository(ICalcsApiClientProxy apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
        }

        public async Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"{buildProjectsUrl}{specificationId}";

            return await _apiClient.GetAsync<BuildProject>(url);
        }

        public async Task<BuildProject> UpdateBuildProjectRelationships(string specificationId, DatasetRelationshipSummary datasetRelationshipSummary)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            Guard.ArgumentNotNull(datasetRelationshipSummary, nameof(datasetRelationshipSummary));

            string url = $"{updateRelationshipsUrl}{specificationId}";

            return await _apiClient.PostAsync<BuildProject, DatasetRelationshipSummary>(url, datasetRelationshipSummary);
        }

        public async Task<IEnumerable<CalculationCurrentVersion>> GetCurrentCalculationsBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"{calculationsUrl}{specificationId}";

            return await _apiClient.GetAsync<IEnumerable<CalculationCurrentVersion>>(url);
        }

        public async Task<HttpStatusCode> CompileAndSaveAssembly(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"calcs/{specificationId}/compileAndSaveAssembly";

            return await _apiClient.GetAsync(url);
        }
    }
}
