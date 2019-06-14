using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Specs.Interfaces;

namespace CalculateFunding.Services.Specs
{
    public class ResultsRepository : IResultsRepository
    {
        const string resultsForSpecificationUrl = "results/get-specification-provider-results?specificationId={0}&top={1}";

        private readonly IResultsApiClientProxy _apiClientProxy;

        public ResultsRepository(IResultsApiClientProxy apiClientProxy)
        {
            _apiClientProxy = apiClientProxy;
        }

        public async Task<bool> SpecificationHasResults(string specificationId)
        {
            string url = string.Format(resultsForSpecificationUrl, specificationId, 1);

            IEnumerable<ProviderResult> providerResults = await _apiClientProxy.GetAsync<IEnumerable<ProviderResult>>(url);
            if (providerResults == null)
            {
                throw new InvalidOperationException("Provider results should not return null, but empty array");
            }

            return providerResults.Any();
        }
    }
}
