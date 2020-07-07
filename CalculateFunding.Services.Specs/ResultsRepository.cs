using System;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Services.Specs.Interfaces;

namespace CalculateFunding.Services.Specs
{
    public class ResultsRepository : IResultsRepository
    {
        private readonly IResultsApiClient _apiClientProxy;

        public ResultsRepository(IResultsApiClient apiClientProxy)
        {
            _apiClientProxy = apiClientProxy;
        }

        public async Task<bool> SpecificationHasResults(string specificationId)
        {
            Common.ApiClient.Models.ApiResponse<bool> providerResults = 
                await _apiClientProxy.GetProviderHasResultsBySpecificationId(specificationId);
            if (providerResults == null)
            {
                throw new InvalidOperationException("Provider results should not return null");
            }

            return providerResults.Content;
        }
    }
}
