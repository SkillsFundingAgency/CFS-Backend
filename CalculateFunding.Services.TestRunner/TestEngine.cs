using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.TestRunner.Interfaces;

namespace CalculateFunding.Services.TestRunner
{
    public class TestEngine
    {
        private readonly IGherkinExecutor _gherkinExecutor;
        private readonly CosmosRepository _cosmosRepository;

        public TestEngine(IGherkinExecutor gherkinExecutor, CosmosRepository cosmosRepository)
        {
            _gherkinExecutor = gherkinExecutor;
            _cosmosRepository = cosmosRepository;
        }

        public async Task RunTests(IEnumerable<TestScenario> testScenarios, IEnumerable<ProviderResult> providerResults, Dictionary<string, ProviderTestResult> currentResults, Specification specification)
        {
            foreach (var providerResult in providerResults)
            {
                var testResult = RunProviderTests(testScenarios, providerResult, null, specification);
                currentResults.TryGetValue(testResult.Provider.Id, out var currenTestResult);
                if (!testResult.Equals(currenTestResult))
                {
                    await _cosmosRepository.CreateAsync(testResult);
                }
            }
        }

        public ProviderTestResult RunProviderTests(IEnumerable<TestScenario> testScenarios, ProviderResult providerResult, IEnumerable<ProviderSourceDataset> providerSourceDatasets, Specification specification)
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
                        _gherkinExecutor.Execute(providerResult, providerSourceDatasets, testScenarios, specification);

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