using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.CalcEngine.Interfaces;

namespace CalculateFunding.Services.CalcEngine
{
    public class CalculationsRepository : ICalculationsRepository
    {
        private readonly IMapper _mapper;
        private readonly ICalculationsApiClient _apiClient;

        public CalculationsRepository(ICalculationsApiClient apiClient, IMapper mapper)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _apiClient = apiClient;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CalculationSummaryModel>> GetCalculationSummariesForSpecification(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            ApiResponse<IEnumerable<Common.ApiClient.Calcs.Models.CalculationSummary>> apiResponse = await _apiClient.GetCalculationSummariesForSpecification(specificationId);

            return _mapper.Map<IEnumerable<CalculationSummaryModel>>(apiResponse?.Content);
        }

        public async Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            ApiResponse<Common.ApiClient.Calcs.Models.BuildProject> apiResponse = await _apiClient.GetBuildProjectBySpecificationId(specificationId);

            return _mapper.Map<BuildProject>(apiResponse?.Content);
        }

        public async Task<byte[]> GetAssemblyBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            ApiResponse<byte[]> apiResponse = await _apiClient.GetAssemblyBySpecificationId(specificationId);

            return apiResponse.Content;
        }
    }
}
