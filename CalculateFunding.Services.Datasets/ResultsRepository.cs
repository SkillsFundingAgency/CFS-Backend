using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Datasets.Interfaces;

namespace CalculateFunding.Services.Datasets
{
    public class ResultsRepository : IResultsRepository
    {
        const string getScopedProviderIdsUrl = "results/get-scoped-providerids?specificationId=";

        private readonly IResultsApiClientProxy _resultsApiClient;

        public ResultsRepository(IResultsApiClientProxy apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _resultsApiClient = apiClient;
        }

        public async Task<IEnumerable<string>> GetAllProviderIdsForSpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                throw new ArgumentNullException(nameof(specificationId));
            }

            string url = $"{getScopedProviderIdsUrl}{specificationId}";

            return await _resultsApiClient.GetAsync<IEnumerable<string>>(url);
        }
    }
}
