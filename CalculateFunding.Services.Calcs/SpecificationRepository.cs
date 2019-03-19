using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class SpecificationRepository : ISpecificationRepository
    {
        const string specsUrl = "specs/specification-summary-by-id?specificationId=";

        private readonly ISpecificationsApiClientProxy _apiClient;

        public SpecificationRepository(ISpecificationsApiClientProxy apiClient)
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


        public Task<IEnumerable<Calculation>> GetCalculationSpecificationsForSpecification(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"specs/calculations-by-specificationid?specificationId={specificationId}";

            return _apiClient.GetAsync<IEnumerable<Calculation>>(url);
        }

        public Task<IEnumerable<FundingStream>> GetFundingStreams()
        {
            string url = $"specs/funding-streams";

            return _apiClient.GetAsync<IEnumerable<FundingStream>>(url);
        }


        public async Task<HttpStatusCode> UpdateCalculationLastUpdatedDate(string specificationId)
        {
            string url = $"specs/update-Calculation-Last-Updated-Date?specificationId={specificationId}";

            return await _apiClient.PostAsync(url);
        }

        public async Task<IEnumerable<SpecificationSummary>> GetAllSpecificationSummaries()
        {
            string url = "specs/specification-summaries";

            return await _apiClient.GetAsync<IEnumerable<SpecificationSummary>>(url);
        }
    }
}
