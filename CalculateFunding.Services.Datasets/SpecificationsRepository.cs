using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Datasets.Interfaces;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets
{
    public class SpecificationsRepository : ISpecificationsRepository
    {
        const string specsUrl = "specs/specifications?specificationId=";

        private readonly IApiClientProxy _apiClient;

        public SpecificationsRepository(IApiClientProxy apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
        }

        public Task<Specification> GetSpecificationById(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"{specsUrl}{specificationId}";

            return _apiClient.GetAsync<Specification>(url);
        }
    }
}
