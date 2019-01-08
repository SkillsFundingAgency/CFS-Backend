using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Results.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class SpecificationsRepository : ISpecificationsRepository
    {
        private const string specsUrl = "specs/specification-summary-by-id?specificationId=";
        private const string allSpecsUrl = "specs/specification-summaries";
        private const string currentSpecsUrl = "specs/specification-current-version-by-id?specificationId=";
        private const string fundingStreamsUrl = "specs/get-fundingstreams";
        private const string fundingPeriodUrl = "specs/get-fundingPeriod-by-id?fundingPeriodId=";
        private const string updatePublishedRefreshedDateUrl = "specs/update-published-refreshed-date?specificationId=";

        private readonly ISpecificationsApiClientProxy _apiClient;

        public SpecificationsRepository(ISpecificationsApiClientProxy apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
        }

        public async Task<SpecificationSummary> GetSpecificationSummaryById(string specificationId)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            string url = $"{specsUrl}{specificationId}";

            return await _apiClient.GetAsync<SpecificationSummary>(url);
        }

        public async Task<SpecificationCurrentVersion> GetCurrentSpecificationById(string specificationId)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            string url = $"{currentSpecsUrl}{specificationId}";

            return await _apiClient.GetAsync<SpecificationCurrentVersion>(url);
        }

        public async Task<IEnumerable<FundingStream>> GetFundingStreams()
        {
            return await _apiClient.GetAsync<IEnumerable<FundingStream>>(fundingStreamsUrl);
        }

        public async Task<Period> GetFundingPeriodById(string fundingPeriodId)
        {
            Guard.ArgumentNotNull(fundingPeriodId, nameof(fundingPeriodId));

            string url = $"{fundingPeriodUrl}{fundingPeriodId}";

            return await _apiClient.GetAsync<Period>(url);
        }

        public async Task<HttpStatusCode> UpdatePublishedRefreshedDate(string specificationId, DateTimeOffset publishedRefreshDate)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            string url = $"{updatePublishedRefreshedDateUrl}{specificationId}";

            UpdatePublishedRefreshedDateModel model = new UpdatePublishedRefreshedDateModel
            {
                PublishedResultsRefreshedAt = publishedRefreshDate
            };

            return await _apiClient.PostAsync(url, model);
        }

        public async Task<IEnumerable<SpecificationSummary>> GetSpecificationSummaries()
        {
            return await _apiClient.GetAsync<IEnumerable<SpecificationSummary>>(allSpecsUrl);
        }
    }
}
