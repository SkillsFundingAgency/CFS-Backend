using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.TestRunner.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class ScenariosRepository : IScenariosRepository
    {
        private readonly IApiClientProxy _apiClient;
        private readonly ICacheProvider _cacheProvider;

        public ScenariosRepository(IApiClientProxy apiClient, ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));

            _apiClient = apiClient;
            _cacheProvider = cacheProvider;
        }

        public async Task<IEnumerable<TestScenario>> GetTestScenariosBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            IEnumerable<TestScenario> testScenarios = await _cacheProvider.GetAsync<List<TestScenario>>(specificationId);

            if (testScenarios.IsNullOrEmpty())
            {
                string url = $"scenarios/get-scenarios-by-specificationId?specificationId={specificationId}";

                testScenarios = await _apiClient.GetAsync<IEnumerable<TestScenario>>(url);

                if (!testScenarios.IsNullOrEmpty())
                {
                    await _cacheProvider.SetAsync<List<TestScenario>>(specificationId, testScenarios.ToList(), TimeSpan.FromHours(1), false);
                }
            }

            return testScenarios;
        }
    }
}
