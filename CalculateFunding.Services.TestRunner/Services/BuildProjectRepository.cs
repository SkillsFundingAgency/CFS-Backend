using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.TestRunner.Interfaces;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class BuildProjectRepository : IBuildProjectRepository
    {
        const string buildProjectUrl = "calcs/get-buildproject-by-specification-id?specificationId=";

        private readonly IApiClientProxy _apiClient;

        public BuildProjectRepository(IApiClientProxy apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
        }

        public Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"{buildProjectUrl}{specificationId}";

            return _apiClient.GetAsync<BuildProject>(url);
        }
    }
}
