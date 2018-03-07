using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class ProviderResultsRepository : IProviderResultsRepository
    {
        const string getProviderResultsUrl = "results/get-provider-results-by-spec-id?specificationId=";

        const string updateProviderResultsUrl = "results/update-provider-results";

        const string getProvidersFromSearch = "results/providers-search";

        private readonly IApiClientProxy _apiClient;

        public ProviderResultsRepository(IApiClientProxy apiClient)
        {
            _apiClient = apiClient;
        }

        public Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"{getProviderResultsUrl}{specificationId}";

            return _apiClient.GetAsync<IEnumerable<ProviderResult>>(url);
        }

        public Task<HttpStatusCode> UpdateProviderResults(IEnumerable<ProviderResult> providerResults)
        {
            return _apiClient.PostAsync(updateProviderResultsUrl, providerResults);
        }

        public Task<ProviderSearchResults> SearchProviders(SearchModel searchModel)
        {
            return _apiClient.PostAsync<ProviderSearchResults, SearchModel>(getProvidersFromSearch, searchModel);
        }
    }
}
