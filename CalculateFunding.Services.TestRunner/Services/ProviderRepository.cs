using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.TestRunner.Interfaces;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class ProviderRepository : IProviderRepository
    {
        const string providerUrl = "results/get-provider-results?providerid={0}&specificationId={1}";

        private readonly IApiClientProxy _apiClient;

        public ProviderRepository(IApiClientProxy apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
        }

        public Task<ProviderResult> GetProviderById(string providerId, string specificationId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentNullException(nameof(providerId));

            string url = string.Format(providerUrl, providerId, specificationId);

            return _apiClient.GetAsync<ProviderResult>(url);
        }
    }
}
