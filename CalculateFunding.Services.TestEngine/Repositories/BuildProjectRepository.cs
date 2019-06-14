using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.TestRunner.Interfaces;

namespace CalculateFunding.Services.TestRunner.Repositories
{
    public class BuildProjectRepository : IBuildProjectRepository
    {
        const string buildProjectUrl = "calcs/get-buildproject-by-specification-id?specificationId=";

        private readonly ICalcsApiClientProxy _apiClient;

        public BuildProjectRepository(ICalcsApiClientProxy apiClient)
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
