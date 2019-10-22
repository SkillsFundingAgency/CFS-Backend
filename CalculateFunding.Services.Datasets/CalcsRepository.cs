using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Datasets.Interfaces;

namespace CalculateFunding.Services.Datasets
{
    public class CalcsRepository : ICalcsRepository
    {
        private IMapper _mapper;

        private readonly ICalculationsApiClient _apiClient;

        public CalcsRepository(ICalculationsApiClient apiClient, IMapper mapper)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

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

        public async Task<BuildProject> UpdateBuildProjectRelationships(string specificationId, DatasetRelationshipSummary datasetRelationshipSummary)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            Guard.ArgumentNotNull(datasetRelationshipSummary, nameof(datasetRelationshipSummary));

            ApiResponse<Common.ApiClient.Calcs.Models.BuildProject> apiResponse = await _apiClient.UpdateBuildProjectRelationships(specificationId, _mapper.Map<Common.ApiClient.Calcs.Models.DatasetRelationshipSummary>(datasetRelationshipSummary));

            return _mapper.Map<BuildProject>(apiResponse?.Content);
        }

        public async Task<IEnumerable<CalculationCurrentVersion>> GetCurrentCalculationsBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            ApiResponse<IEnumerable<Common.ApiClient.Calcs.Models.CalculationCurrentVersion>> apiResponse = await _apiClient.GetCurrentCalculationsBySpecificationId(specificationId);

            return _mapper.Map<IEnumerable<CalculationCurrentVersion>>(apiResponse?.Content);
        }

        public async Task<HttpStatusCode> CompileAndSaveAssembly(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            ApiResponse<HttpStatusCode> apiResponse = await _apiClient.CompileAndSaveAssembly(specificationId);

            return apiResponse.Content;
        }
    }
}
