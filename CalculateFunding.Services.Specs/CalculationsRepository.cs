using System;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Specs.Interfaces;

namespace CalculateFunding.Services.Specs
{
    [Obsolete("Replace with common nuget API client")]
    public class CalculationsRepository : ICalculationsRepository
    {
        const string validateCalculationNameUrl = "calcs/validate-calc-name/{0}/{1}/{2}";

        private readonly ICalculationsApiClient _apiClientProxy;

        public CalculationsRepository(ICalculationsApiClient apiClientProxy)
        {
            Guard.ArgumentNotNull(apiClientProxy, nameof(apiClientProxy));

            _apiClientProxy = apiClientProxy;
        }

        public async Task<bool> IsCalculationNameValid(string specificationId, string calculationName, string existingCalculationId = null)
        {
            ApiResponse<bool> apiResponse = await _apiClientProxy.IsCalculationNameValid(specificationId, calculationName, existingCalculationId);

            return apiResponse.Content;
        }
    }
}
