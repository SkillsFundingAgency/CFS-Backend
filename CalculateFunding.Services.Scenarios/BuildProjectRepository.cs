using System;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Scenarios.Interfaces;

namespace CalculateFunding.Services.Scenarios
{
    public class BuildProjectRepository : IBuildProjectRepository
    {
        private readonly IMapper _mapper;
        private readonly ICalculationsApiClient _apiClient;

        public BuildProjectRepository(ICalculationsApiClient apiClient, IMapper mapper)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
            _mapper = mapper;
        }

        public async Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            ApiResponse<Common.ApiClient.Calcs.Models.BuildProject> apiResponse = await _apiClient.GetBuildProjectBySpecificationId(specificationId);

            return _mapper.Map<BuildProject>(apiResponse?.Content);
        }
    }
}
