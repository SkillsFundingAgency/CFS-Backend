using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.TestRunner.Interfaces;

namespace CalculateFunding.Services.TestRunner.Repositories
{
    public class ScenariosRepository : IScenariosRepository, IHealthChecker
    {
        private readonly IScenariosApiClientProxy _apiClient;
        private readonly ICacheProvider _cacheProvider;

        public ScenariosRepository(IScenariosApiClientProxy apiClient, ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));

            _apiClient = apiClient;
            _cacheProvider = cacheProvider;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            var cacheHealth = await _cacheProvider.IsHealthOk();

            health.Name = nameof(ScenariosRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheHealth.Ok, DependencyName = this.GetType().GetFriendlyName(), Message = cacheHealth.Message });

            return health;
        }

        public async Task<IEnumerable<TestScenario>> GetTestScenariosBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            IEnumerable<TestScenario> testScenarios = await _cacheProvider.GetAsync<List<TestScenario>>($"{CacheKeys.TestScenarios}{specificationId}");

            if (testScenarios.IsNullOrEmpty())
            {
                string url = $"scenarios/get-scenarios-by-specificationId?specificationId={specificationId}";

                testScenarios = await _apiClient.GetAsync<IEnumerable<TestScenario>>(url);

                if (!testScenarios.IsNullOrEmpty())
                {
                    await _cacheProvider.SetAsync<List<TestScenario>>($"{CacheKeys.TestScenarios}{specificationId}", testScenarios.ToList(), TimeSpan.FromHours(1), false);
                }
            }

            return testScenarios;
        }
    }
}
