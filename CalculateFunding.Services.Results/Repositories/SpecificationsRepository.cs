using System;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Results.Interfaces;

namespace CalculateFunding.Services.Results.Repositories
{
    [Obsolete("Replace with common nuget API client")]
    public class SpecificationsRepository : ISpecificationsRepository
    {
        private const string currentSpecsUrl = "specs/specification-current-version-by-id?specificationId=";

        private const string updatePublishedRefreshedDateUrl = "specs/update-published-refreshed-date?specificationId=";

        private readonly ISpecificationsApiClientProxy _apiClient;

        public SpecificationsRepository(ISpecificationsApiClientProxy apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
        }

        public async Task<SpecificationCurrentVersion> GetCurrentSpecificationById(string specificationId)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            string url = $"{currentSpecsUrl}{specificationId}";

            return await _apiClient.GetAsync<SpecificationCurrentVersion>(url);
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
    }
}
