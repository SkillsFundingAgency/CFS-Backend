using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Proxies;

namespace CalculateFunding.Services.Calculator
{
    public class CalculationsRepository : ICalculationsRepository
    {
        private readonly ICalcsApiClientProxy _apiClient;

        public CalculationsRepository(ICalcsApiClientProxy apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
        }

        public Task<IEnumerable<CalculationSummaryModel>> GetCalculationSummariesForSpecification(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"calcs/calculation-summaries-for-specification?specificationId={specificationId}";

            return _apiClient.GetAsync<IEnumerable<CalculationSummaryModel>>(url);
        }

        public Task<BuildProject> GetBuildProjectBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"calcs/get-buildproject-by-specification-id?specificationId={specificationId}";

            return _apiClient.GetAsync<BuildProject>(url);
        }

        public Task<byte[]> GetAssemblyBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"calcs/{specificationId}/assembly";

            return _apiClient.GetAsync<byte[]>(url);
        }
    }
}
