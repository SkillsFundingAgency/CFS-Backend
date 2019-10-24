using System;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Proxies;

namespace CalculateFunding.Services.Calcs
{
    [Obsolete("Replace with common nuget API client")]
    public class SpecificationRepository : ISpecificationRepository
    {
        private readonly ISpecificationsApiClientProxy _apiClient;

        public SpecificationRepository(ISpecificationsApiClientProxy apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
        }

        public async Task<HttpStatusCode> UpdateCalculationLastUpdatedDate(string specificationId)
        {
            string url = $"specs/update-Calculation-Last-Updated-Date?specificationId={specificationId}";

            return await _apiClient.PostAsync(url);
        }

    }
}
