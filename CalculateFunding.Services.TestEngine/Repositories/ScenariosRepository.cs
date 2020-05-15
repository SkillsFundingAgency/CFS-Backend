using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Common.ApiClient.Scenarios;

namespace CalculateFunding.Services.TestRunner.Repositories
{
    public class ScenariosRepository : IScenariosRepository, IHealthChecker
    {
        private readonly IScenariosApiClient _apiClient;
        private readonly ICacheProvider _cacheProvider;
        private readonly IMapper _mapper;

        public ScenariosRepository(IScenariosApiClient apiClient, ICacheProvider cacheProvider, IMapper mapper)
        {
            Guard.ArgumentNotNull(apiClient, nameof(apiClient));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _apiClient = apiClient;
            _cacheProvider = cacheProvider;
            _mapper = mapper;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            (bool Ok, string Message) = await _cacheProvider.IsHealthOk();

            health.Name = nameof(ScenariosRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = GetType().GetFriendlyName(), Message = Message });

            return health;
        }

        public async Task<IEnumerable<TestScenario>> GetTestScenariosBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            IEnumerable<TestScenario> testScenarios = await _cacheProvider.GetAsync<List<TestScenario>>($"{CacheKeys.TestScenarios}{specificationId}");

            if (testScenarios.IsNullOrEmpty())
            {
                ApiResponse<IEnumerable<CalculateFunding.Common.ApiClient.Scenarios.Models.TestScenario>> apiClientResponse = await _apiClient.GetTestScenariosBySpecificationId(specificationId);

                if (!apiClientResponse.StatusCode.IsSuccess())
                {
                    string message = $"No Test Scenario found for specificationId '{specificationId}'.";
                    throw new RetriableException(message);
                }

                testScenarios = _mapper.Map<IEnumerable<TestScenario>>(apiClientResponse.Content);

                if (!testScenarios.IsNullOrEmpty())
                {
                    await _cacheProvider.SetAsync<List<TestScenario>>($"{CacheKeys.TestScenarios}{specificationId}", testScenarios.ToList(), TimeSpan.FromHours(1), false);
                }
            }

            return testScenarios;
        }
    }
}
