using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.TestRunner.Interfaces;
using Serilog;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.TestRunner
{
    public class TestEngine : ITestEngine, IHealthChecker
    {
        private readonly IGherkinExecutor _gherkinExecutor;
        private readonly ILogger _logger;

        public TestEngine(IGherkinExecutor gherkinExecutor, ILogger logger)
        {
            Guard.ArgumentNotNull(gherkinExecutor, nameof(gherkinExecutor));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _gherkinExecutor = gherkinExecutor;
            _logger = logger;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth gherkinHealth = await ((IHealthChecker)_gherkinExecutor).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(TestEngine)
            };
            health.Dependencies.AddRange(gherkinHealth.Dependencies);

            return health;
        }

        public async Task<IEnumerable<TestScenarioResult>> RunTests(IEnumerable<TestScenario> testScenarios, IEnumerable<ProviderResult> providerResults,
            IEnumerable<ProviderSourceDataset> sourceDatasets, IEnumerable<TestScenarioResult> currentResults, SpecModel.SpecificationSummary specification, BuildProject buildProject)
        {
            Guard.ArgumentNotNull(testScenarios, nameof(testScenarios));
            Guard.ArgumentNotNull(providerResults, nameof(providerResults));
            Guard.ArgumentNotNull(sourceDatasets, nameof(sourceDatasets));
            Guard.ArgumentNotNull(currentResults, nameof(currentResults));
            Guard.ArgumentNotNull(specification, nameof(specification));
            Guard.ArgumentNotNull(buildProject, nameof(buildProject));

            IList<TestScenarioResult> scenarioResults = new List<TestScenarioResult>();

            if (!providerResults.Any())
            {
                _logger.Warning("No provider results were supplied to execute tests");
            }
            else if (!testScenarios.Any())
            {
                _logger.Warning("No test scenarios were supplied to execute tests");
            }
            else
            {
                foreach (ProviderResult providerResult in providerResults)
                {
                    IEnumerable<ProviderSourceDataset> providerSourceDatasets = sourceDatasets.Where(m => m.ProviderId == providerResult.Provider.Id);

                    IEnumerable<ScenarioResult> testResults = await RunTests(testScenarios, providerResult, providerSourceDatasets, buildProject);

                    if (!testResults.IsNullOrEmpty())
                    {
                        foreach (ScenarioResult testResult in testResults)
                        {
                            TestResult status = (testResult.StepsExecuted == 0 || testResult.StepsExecuted < testResult.TotalSteps)
                                        ? TestResult.Ignored
                                        : testResult.HasErrors
                                            ? TestResult.Failed
                                            : TestResult.Passed;

                            TestScenarioResult filteredCurrentResults = currentResults.FirstOrDefault(m => m.Provider.Id == providerResult.Provider.Id && m.TestScenario.Id == testResult.Scenario.Id && m.TestResult == status);

                            if (filteredCurrentResults == null)
                            {
                                scenarioResults.Add(new TestScenarioResult
                                {
                                    TestResult = status,
                                    Specification = new Reference(specification.Id, specification.Name),
                                    TestScenario = new Reference(testResult.Scenario.Id, testResult.Scenario.Name),
                                    Provider = new Reference(providerResult.Provider.Id, providerResult.Provider.Name)
                                });
                            }
                        }
                    }
                    else
                    {
                        _logger.Warning($"No test results generated for provider: {providerResult.Provider?.Id} on specification: {providerResult.SpecificationId}");
                    }
                }
            }
            return scenarioResults;
        }

        private async Task<IEnumerable<ScenarioResult>> RunTests(IEnumerable<TestScenario> testScenarios, ProviderResult providerResult,
            IEnumerable<ProviderSourceDataset> providerSourceDatasets, BuildProject buildProject)
        {
            List<ScenarioResult> scenarioResults = new List<ScenarioResult>();

            if (testScenarios != null)
            {
                IEnumerable<ScenarioResult> gherkinScenarioResults =
                    await _gherkinExecutor.Execute(providerResult, providerSourceDatasets, testScenarios, buildProject);

                if (!gherkinScenarioResults.IsNullOrEmpty())
                {
                    scenarioResults.AddRange(gherkinScenarioResults);
                }
            }

            return scenarioResults;
        }
    }
}