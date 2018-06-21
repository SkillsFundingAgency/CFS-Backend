using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.TestRunner.Interfaces;

namespace CalculateFunding.Services.TestRunner
{
    public class TestEngine : ITestEngine
    {
        private readonly IGherkinExecutor _gherkinExecutor;
        private readonly ITelemetry _telemetry;

        public TestEngine(IGherkinExecutor gherkinExecutor,
            ITelemetry telemetry)
        {
            _gherkinExecutor = gherkinExecutor;
            _telemetry = telemetry;
        }

        public async Task<IEnumerable<TestScenarioResult>> RunTests(IEnumerable<TestScenario> testScenarios, IEnumerable<ProviderResult> providerResults,
            IEnumerable<ProviderSourceDatasetCurrent> sourceDatasets, IEnumerable<TestScenarioResult> currentResults, SpecificationSummary specification, BuildProject buildProject)
        {
            IList<TestScenarioResult> scenarioResults = new List<TestScenarioResult>();


            foreach (var providerResult in providerResults)
            {
                var providerSourceDatasets = sourceDatasets.Where(m => m.ProviderId == providerResult.Provider.Id);

                var testResults = await RunTests(testScenarios, providerResult, providerSourceDatasets, buildProject);

                if (!testResults.IsNullOrEmpty())
                {
                    foreach (var testResult in testResults)
                    {
                        var status = (testResult.StepsExecuted == 0 || testResult.StepsExecuted < testResult.TotalSteps)
                                    ? TestResult.Ignored
                                    : testResult.HasErrors
                                        ? TestResult.Failed
                                        : TestResult.Passed;

                        var filteredCurrentResults = currentResults.FirstOrDefault(m => m.Provider.Id == providerResult.Provider.Id && m.TestScenario.Id == testResult.Scenario.Id && m.TestResult == status);

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

            }
            return scenarioResults;
        }

        async Task<IEnumerable<ScenarioResult>> RunTests(IEnumerable<TestScenario> testScenarios, ProviderResult providerResult,
            IEnumerable<ProviderSourceDatasetCurrent> providerSourceDatasets, BuildProject buildProject)
        {
            List<ScenarioResult> scenarioResults = new List<ScenarioResult>();

            if (testScenarios != null)
            {
                var gherkinScenarioResults =
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