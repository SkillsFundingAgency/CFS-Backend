using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.TestRunner.Interfaces;
using Newtonsoft.Json;
using Polly;

namespace CalculateFunding.Services.TestRunner
{
    public class GherkinExecutor : IGherkinExecutor, IHealthChecker
    {
        private readonly IGherkinParser _parser;
        private readonly ICacheProvider _cacheProvider;
        private readonly Policy _cacheProviderPolicy;

        public GherkinExecutor(IGherkinParser parser,
            ICacheProvider cacheProvider,
            ITestRunnerResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(parser, nameof(parser));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));

            _parser = parser;
            _cacheProvider = cacheProvider;

            _cacheProviderPolicy = resiliencePolicies.CacheProviderRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            (bool Ok, string Message) cacheHealth = await _cacheProvider.IsHealthOk();

            health.Name = nameof(GherkinExecutor);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheHealth.Ok, DependencyName = this.GetType().Name, Message = cacheHealth.Message });

            return health;
        }

        public async Task<IEnumerable<ScenarioResult>> Execute(ProviderResult providerResult, IEnumerable<ProviderSourceDataset> datasets,
            IEnumerable<TestScenario> testScenarios, BuildProject buildProject)
        {
            Guard.ArgumentNotNull(providerResult, nameof(providerResult));
            Guard.ArgumentNotNull(datasets, nameof(datasets));
            Guard.ArgumentNotNull(testScenarios, nameof(testScenarios));
            Guard.ArgumentNotNull(buildProject, nameof(buildProject));

            IList<ScenarioResult> scenarioResults = new List<ScenarioResult>();

            foreach (TestScenario scenario in testScenarios)
            {
                ScenarioResult scenarioResult = new ScenarioResult
                {
                    Scenario = new Reference(scenario.Id, scenario.Name)
                };

                GherkinParseResult parseResult = await GetGherkinParseResult(scenario, buildProject);

                if (parseResult != null && !parseResult.StepActions.IsNullOrEmpty())
                {
                    scenarioResult.TotalSteps = parseResult.StepActions.Count;

                    scenarioResult.StepsExecuted = 0;

                    foreach (IStepAction action in parseResult.StepActions)
                    {
                        GherkinParseResult result = action.Execute(providerResult, datasets);

                        if (result.Abort)
                        {
                            break;
                        }

                        if (!result.Dependencies.IsNullOrEmpty())
                        {
                            foreach (Dependency resultDependency in result.Dependencies)
                            {
                                if (!scenarioResult.Dependencies.Contains(resultDependency))
                                {
                                    scenarioResult.Dependencies.Add(resultDependency);
                                }
                            }
                        }
                        if (result.HasErrors)
                        {
                            scenarioResult.Errors.AddRange(result.Errors);
                        }

                        scenarioResult.StepsExecuted++;
                    }
                }

                scenarioResults.Add(scenarioResult);
            }
            return scenarioResults;
        }

        private async Task<GherkinParseResult> GetGherkinParseResult(TestScenario testScenario, BuildProject buildProject)
        {
            Guard.ArgumentNotNull(testScenario, nameof(testScenario));
            Guard.ArgumentNotNull(buildProject, nameof(buildProject));

            string cacheKey = $"{CacheKeys.GherkinParseResult}{testScenario.Id}";

            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };

            GherkinParseResult gherkinParseResult = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<GherkinParseResult>(cacheKey, jsonSerializerSettings));

            if (gherkinParseResult == null)
            {
                gherkinParseResult = await _parser.Parse(testScenario.SpecificationId, testScenario.Current.Gherkin, buildProject);

                if (gherkinParseResult != null && !gherkinParseResult.StepActions.IsNullOrEmpty())
                {
                    await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync<GherkinParseResult>(cacheKey, gherkinParseResult, TimeSpan.FromHours(24), true, jsonSerializerSettings));
                }
            }

            return gherkinParseResult;
        }
    }
}
