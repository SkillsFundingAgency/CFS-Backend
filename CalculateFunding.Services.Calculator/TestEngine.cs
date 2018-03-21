using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.TestRunner;
using CalculateFunding.Services.TestRunner.Vocab;

namespace CalculateFunding.Services.Calculator
{
    public class TestEngine
    {
        private readonly GherkinExecutor _gherkinExecutor;

        public TestEngine(GherkinExecutor gherkinExecutor)
        {
            _gherkinExecutor = gherkinExecutor;
        }

        public async Task RunTests(CosmosRepository repository, List<TestScenario> testScenarios, IEnumerable<ProviderResult> providerResults, Dictionary<string, ProviderTestResult> currentResults)
        {
            foreach (var providerResult in providerResults)
            {
                var testResult = RunProviderTests(testScenarios, providerResult, null);
                currentResults.TryGetValue(testResult.Provider.Id, out var currenTestResult);
                if (!testResult.Equals(currenTestResult))
                {
                    await repository.CreateAsync(testResult);
                }
                
            }

        }
        public ProviderTestResult RunProviderTests(List<TestScenario> testScenarios, ProviderResult providerResult, List<ProviderSourceDataset> providerSourceDatasets)
        {


            var testResult = new ProviderTestResult
            {
                Provider = providerResult.Provider,
                Budget = providerResult.Specification,
            };
            var scenarioResults = new List<ProductTestScenarioResult>();
            foreach (var productResult in providerResult.CalculationResults)
            {
                if (testScenarios != null)
                {
                    var gherkinScenarioResults =
                        _gherkinExecutor.Execute(providerResult, providerSourceDatasets, testScenarios);

                    foreach (var executeResult in gherkinScenarioResults)
                    {
                        scenarioResults.Add(new ProductTestScenarioResult
                        {
                            Calculation = productResult.Calculation,
                            CalculationSpecification = productResult.CalculationSpecification,
                            AllocationLine = productResult.AllocationLine,
                            PolicySpecifications = productResult.PolicySpecifications,
                            Value = productResult.Value,
                            Scenario = executeResult.Scenario,
                            TestResult =
                                executeResult.StepsExecuted < executeResult.TotalSteps
                                    ? TestResult.Ignored
                                    : executeResult.HasErrors
                                        ? TestResult.Failed
                                        : TestResult.Passed,
                            StepExected = executeResult.StepsExecuted,
                            TotalSteps = executeResult.TotalSteps,
                            DatasetReferences = executeResult.Dependencies.Select(x => new DatasetReference
                            {
                                DatasetName = x.DatasetName,
                                FieldName = x.FieldName,
                                Value = x.Value
                            }).ToArray()
                        });
                    }
                }
            }

            testResult.ScenarioResults = scenarioResults.ToArray();
            return testResult;
        }
    }
}