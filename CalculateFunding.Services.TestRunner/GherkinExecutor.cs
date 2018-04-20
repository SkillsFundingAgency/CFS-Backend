using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.TestRunner.Interfaces;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Interfaces.Caching;
using System;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Services.Core.Helpers;
using Polly;
using System.Diagnostics;
using Newtonsoft.Json;
using CalculateFunding.Services.Core.Caching;

namespace CalculateFunding.Services.TestRunner
{
    public class GherkinExecutor : IGherkinExecutor
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

        async Task<GherkinParseResult> GetGherkinParseResult(TestScenario testScenario, BuildProject buildProject)
        {

            string cacheKey = $"{CacheKeys.GherkinParseResult}{testScenario.Id}";

            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.All
            };

            GherkinParseResult gherkinParseResult = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<GherkinParseResult>(cacheKey, jsonSerializerSettings));
            if (gherkinParseResult == null)
            {
                gherkinParseResult = await _parser.Parse(testScenario.Current.Gherkin, buildProject);

                if (gherkinParseResult != null && !gherkinParseResult.StepActions.IsNullOrEmpty())
                {
                    await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync<GherkinParseResult>(cacheKey, gherkinParseResult, TimeSpan.FromHours(24), true, jsonSerializerSettings));
                }
            }

            return gherkinParseResult;
        }

        public async Task<IEnumerable<ScenarioResult>> Execute(ProviderResult providerResult, IEnumerable<ProviderSourceDataset> datasets, IEnumerable<TestScenario> testScenarios, BuildProject buildProject)
        {
            IList<ScenarioResult> scenarioResults = new List<ScenarioResult>();

            foreach (var scenario in testScenarios)
            {
                var scenarioResult = new ScenarioResult
                {
                    Scenario = new Reference(scenario.Id, scenario.Name)
                };

                var parseResult = await GetGherkinParseResult(scenario, buildProject);

                if (parseResult != null && !parseResult.StepActions.IsNullOrEmpty())
                {
                    scenarioResult.TotalSteps = parseResult.StepActions.Count;

                    scenarioResult.StepsExecuted = 0;
                   
                    foreach (var action in parseResult.StepActions)
                    {
                        GherkinParseResult result = action.Execute(providerResult, datasets);

                        if (result.Abort)
                        {
                            break;
                        }

                        if (!result.Dependencies.IsNullOrEmpty())
                        {
                            foreach (var resultDependency in result.Dependencies)
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
    }
}
