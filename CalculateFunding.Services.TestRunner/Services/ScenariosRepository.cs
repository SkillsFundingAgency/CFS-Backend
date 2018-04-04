using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.TestRunner.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class ScenariosRepository : IScenariosRepository
    {
        private readonly IApiClientProxy _apiClient;

        public ScenariosRepository(IApiClientProxy apiClient)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));

            _apiClient = apiClient;
        }

        public Task<IEnumerable<TestScenario>> GetTestScenariosBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string url = $"scenarios/get-scenarios-by-specificationId?specificationId={specificationId}";

            return _apiClient.GetAsync<IEnumerable<TestScenario>>(url);
        }
    }
}
