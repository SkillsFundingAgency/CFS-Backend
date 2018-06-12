using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Results.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class SpecificationsRepository : ISpecificationsRepository
    {
        const string specsUrl = "specs/specification-summary-by-id?specificationId=";

        const string currentSpecsUrl = "specs/specification-current-version-by-id?specificationId=";

        const string fundingStreamsUrl = "specs/get-fundingstreams";

        private readonly IApiClientProxy _apiClient;

        public SpecificationsRepository(IApiClientProxy apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
        }

        public Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"{specsUrl}{specificationId}";

            return _apiClient.GetAsync<SpecificationSummary>(url);
        }

        public Task<SpecificationCurrentVersion> GetCurrentSpecificationById(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"{currentSpecsUrl}{specificationId}";

            return _apiClient.GetAsync<SpecificationCurrentVersion>(url);
        }

        public Task<IEnumerable<FundingStream>> GetFundingStreams()
        {
            return _apiClient.GetAsync<IEnumerable<FundingStream>>(fundingStreamsUrl);
        }
    }
}
