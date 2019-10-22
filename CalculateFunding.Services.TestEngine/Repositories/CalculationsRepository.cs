using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.TestEngine.Interfaces;

namespace CalculateFunding.Services.TestRunner.Repositories
{
    public class CalculationsRepository : ICalculationsRepository
    {
        private readonly ICalculationsApiClient _apiClient;

        public CalculationsRepository(ICalculationsApiClient apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
        }

        public async Task<byte[]> GetAssemblyBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            ApiResponse<byte[]> apiResponse = await _apiClient.GetAssemblyBySpecificationId(specificationId);

            return apiResponse?.Content;
        }
    }
}
