using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.TestRunner.Interfaces;

namespace CalculateFunding.Services.TestRunner
{
    public class TestEngine : ITestEngine
    {
        private readonly IGherkinExecutor _gherkinExecutor;

        public TestEngine(IGherkinExecutor gherkinExecutor)
        {
            _gherkinExecutor = gherkinExecutor;
        }

        public async Task<IEnumerable<TestScenarioResult>> RunTests(IEnumerable<TestScenario> testScenarios, IEnumerable<ProviderResult> providerResults, 
            IEnumerable<ProviderSourceDataset> sourceDatasets, IEnumerable<TestScenarioResult> currentResults, Specification specification, BuildProject buildProject)
        {
            IList<TestScenarioResult> scenarioResults = new List<TestScenarioResult>();

            foreach (var providerResult in providerResults)
            {
                var providerSourceDatasets = sourceDatasets.Where(m => m.Provider.Id == providerResult.Provider.Id);

                var testResults = RunTests(testScenarios, providerResult, providerSourceDatasets, buildProject);

                if (!testResults.IsNullOrEmpty())
                {
                    foreach (var testResult in testResults)
                    {
                        var status = testResult.StepsExecuted < testResult.TotalSteps
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

        //public ProviderTestResult RunProviderTests(IEnumerable<TestScenario> testScenarios, ProviderResult providerResult, IEnumerable<ProviderSourceDataset> providerSourceDatasets, Specification specification)
        //{
        //    var testResult = new ProviderTestResult
        //    {
        //        Provider = providerResult.Provider,
        //        Budget = providerResult.Specification,
        //    };
        //    var scenarioResults = new List<ProductTestScenarioResult>();
        //    foreach (var productResult in providerResult.CalculationResults)
        //    {
        //        if (testScenarios != null)
        //        {
        //            var gherkinScenarioResults =
        //                _gherkinExecutor.Execute(providerResult, providerSourceDatasets, testScenarios, specification);

        //            foreach (var executeResult in gherkinScenarioResults)
        //            {
        //                scenarioResults.Add(new ProductTestScenarioResult
        //                {
        //                    Calculation = productResult.Calculation,
        //                    CalculationSpecification = productResult.CalculationSpecification,
        //                    AllocationLine = productResult.AllocationLine,
        //                    PolicySpecifications = productResult.PolicySpecifications,
        //                    Value = productResult.Value,
        //                    Scenario = executeResult.Scenario,
        //                    TestResult =
        //                        executeResult.StepsExecuted < executeResult.TotalSteps
        //                            ? TestResult.Ignored
        //                            : executeResult.HasErrors
        //                                ? TestResult.Failed
        //                                : TestResult.Passed,
        //                    StepExected = executeResult.StepsExecuted,
        //                    TotalSteps = executeResult.TotalSteps,
        //                    DatasetReferences = executeResult.Dependencies.Select(x => new DatasetReference
        //                    {
        //                        DatasetName = x.DatasetName,
        //                        FieldName = x.FieldName,
        //                        Value = x.Value
        //                    }).ToArray()
        //                });
        //            }
        //        }
        //    }

        //    testResult.ScenarioResults = scenarioResults.ToArray();

        //    return testResult;
        //}

        IEnumerable<ScenarioResult> RunTests(IEnumerable<TestScenario> testScenarios, ProviderResult providerResult, 
            IEnumerable<ProviderSourceDataset> providerSourceDatasets, BuildProject buildProject)
        {
            var scenarioResults = new List<ScenarioResult>();

            foreach (var productResult in providerResult.CalculationResults)
            {
                if (testScenarios != null)
                {
                    var gherkinScenarioResults =
                        _gherkinExecutor.Execute(providerResult, providerSourceDatasets, testScenarios, buildProject);

                    if (!gherkinScenarioResults.IsNullOrEmpty())
                    {
                        scenarioResults.AddRange(gherkinScenarioResults);
                    }
                }
            }

            return scenarioResults;
        }
    }
}