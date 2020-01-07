using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Specs.Interfaces;

namespace CalculateFunding.Services.Specs
{
    public class ResultsRepository : IResultsRepository
    {
        const string resultsForSpecificationUrl = "results/get-specification-provider-results?specificationId={0}&top={1}";

        private readonly IResultsApiClient _apiClientProxy;

        public ResultsRepository(IResultsApiClient apiClientProxy)
        {
            _apiClientProxy = apiClientProxy;
        }

        public async Task<bool> SpecificationHasResults(string specificationId)
        {
            string url = string.Format(resultsForSpecificationUrl, specificationId, 1);

            Common.ApiClient.Models.ApiResponse<IEnumerable<Common.ApiClient.Results.Models.ProviderResult>> providerResults = await _apiClientProxy.GetProviderResultsBySpecificationId(specificationId,"1");
            if (providerResults == null)
            {
                throw new InvalidOperationException("Provider results should not return null");
            }

            if (providerResults.Content == null)
            {
                throw new InvalidOperationException("Provider results content should not return null");
            }

            return providerResults.Content.Any();
        }
    }
}
