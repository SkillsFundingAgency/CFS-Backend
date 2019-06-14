using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.Scenarios.Interfaces;

namespace CalculateFunding.Services.Scenarios
{
    public class CalcsRepository : ICalcsRepository
    {
        const string calculationsUrl = "calcs/current-calculations-for-specification?specificationId=";

        private readonly ICalcsApiClientProxy _apiClient;

        public CalcsRepository(ICalcsApiClientProxy apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
        }

        public async Task<IEnumerable<CalculationCurrentVersion>> GetCurrentCalculationsBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                throw new ArgumentNullException(nameof(specificationId));
            }

            string url = $"{calculationsUrl}{specificationId}";

            return await _apiClient.GetAsync<IEnumerable<CalculationCurrentVersion>>(url);
        }
    }
}
