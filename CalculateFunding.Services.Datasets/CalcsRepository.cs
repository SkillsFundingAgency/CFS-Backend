using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Datasets.Interfaces;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets
{
    public class CalcsRepository : ICalcsRepository
    {
        const string buildProjectsUrl = "calcs/get-buildproject-by-specification-id?specificationId=";

        private readonly IApiClientProxy _apiClient;

        public CalcsRepository(IApiClientProxy apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
        }

        public Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"{buildProjectsUrl}{specificationId}";

            return _apiClient.GetAsync<BuildProject>(url);
        }
    }
}
