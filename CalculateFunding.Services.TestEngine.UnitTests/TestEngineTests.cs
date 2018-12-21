using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.TestRunner.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.TestRunner.UnitTests
{
    [TestClass]
    public class TestEngineTests
    {
        const string ProviderId = "provider-id-1";
        const string SpecificationId = "pspec-id-1";

        [TestMethod]
        public async Task RunTests_GivenNoProviders_LogsAndreturnsEmptyResults()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = new ProviderResult[0];
            IEnumerable<TestScenario> scenarios = new TestScenario[0];
            IEnumerable<ProviderSourceDataset> providerSourceDatasets = new ProviderSourceDataset[0];
            IEnumerable<TestScenarioResult> testScenarioResults = new TestScenarioResult[0];
            SpecificationSummary specificationSummary = new SpecificationSummary();
            BuildProject buildProject = new BuildProject();

            ILogger logger = CreateLogger();

            TestEngine testEngine = CreateTestEngine(logger: logger);

            //Act
            IEnumerable<TestScenarioResult> results = await testEngine.RunTests(scenarios, providerResults, providerSourceDatasets,
                testScenarioResults, specificationSummary, buildProject);

            results
                .Count()
                .Should()
                .Be(0);

            logger
                .Received(1)
                .Warning(Arg.Is("No provider results were supplied to execute tests"));
        }

        [TestMethod]
        public async Task RunTests_GivenNoTestScenarios_LogsAndreturnsEmptyResults()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = new[] { new ProviderResult() };
            IEnumerable<TestScenario> scenarios = new TestScenario[0];
            IEnumerable<ProviderSourceDataset> providerSourceDatasets = new ProviderSourceDataset[0];
            IEnumerable<TestScenarioResult> testScenarioResults = new TestScenarioResult[0];
            SpecificationSummary specificationSummary = new SpecificationSummary();
            BuildProject buildProject = new BuildProject();

            ILogger logger = CreateLogger();

            TestEngine testEngine = CreateTestEngine(logger: logger);

            //Act
            IEnumerable<TestScenarioResult> results = await testEngine.RunTests(scenarios, providerResults, providerSourceDatasets,
                testScenarioResults, specificationSummary, buildProject);

            results
                .Count()
                .Should()
                .Be(0);

            logger
                .Received(1)
                .Warning(Arg.Is("No test scenarios were supplied to execute tests"));
        }

        [TestMethod]
        public async Task RunTests_GivenNoTestResults_LogsAndreturnsEmptyResults()
        {
            //Arrange
            IEnumerable<ProviderResult> providerResults = new[] { new ProviderResult { Provider = new ProviderSummary { Id = ProviderId }, SpecificationId = SpecificationId } };
            IEnumerable<TestScenario> scenarios = new[] { new TestScenario() };
            IEnumerable<ProviderSourceDataset> providerSourceDatasets = new ProviderSourceDataset[0];
            IEnumerable<TestScenarioResult> testScenarioResults = new TestScenarioResult[0];
            SpecificationSummary specificationSummary = new SpecificationSummary();
            BuildProject buildProject = new BuildProject();

            ILogger logger = CreateLogger();

            TestEngine testEngine = CreateTestEngine(logger: logger);

            //Act
            IEnumerable<TestScenarioResult> results = await testEngine.RunTests(scenarios, providerResults, providerSourceDatasets,
                testScenarioResults, specificationSummary, buildProject);

            results
                .Count()
                .Should()
                .Be(0);

            logger
                .Received(1)
                .Warning(Arg.Is($"No test results generated for provider: {ProviderId} on specification: {SpecificationId}"));
        }

        [TestMethod]
        public async Task RunTests_GivenTestResultsReturnedFromExecutorWhereNoStepsExecuted_ReturnsOneIgnoreTestResult()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult { Provider = new ProviderSummary { Id = ProviderId, Name = "any provider" }, SpecificationId = SpecificationId };

            IEnumerable<ProviderResult> providerResults = new[] { providerResult };
            IEnumerable<TestScenario> scenarios = new[] { new TestScenario() };
            IEnumerable<ProviderSourceDataset> providerSourceDatasets = new ProviderSourceDataset[0];
            IEnumerable<TestScenarioResult> testScenarioResults = new TestScenarioResult[0];
            SpecificationSummary specificationSummary = new SpecificationSummary { Id = SpecificationId, Name = "spec-name" };
            BuildProject buildProject = new BuildProject();

            IEnumerable<ScenarioResult> scenarioResults = new[]
            {
                new ScenarioResult  { Scenario = new Reference("sceanrio=id", "scenario name") }
            };

            ILogger logger = CreateLogger();

            IGherkinExecutor gherkinExecutor = CreateGherkinExecutor();
            gherkinExecutor
                .Execute(Arg.Any<ProviderResult>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<TestScenario>>(), Arg.Any<BuildProject>())
                .Returns(scenarioResults);

            TestEngine testEngine = CreateTestEngine(gherkinExecutor, logger);

            //Act
            IEnumerable<TestScenarioResult> results = await testEngine.RunTests(scenarios, providerResults, providerSourceDatasets,
                testScenarioResults, specificationSummary, buildProject);

            results
                .Count()
                .Should()
                .Be(1);

            results
                .First()
                .TestResult
                .Should()
                .Be(Models.Results.TestResult.Ignored);
        }



        static TestEngine CreateTestEngine(IGherkinExecutor gherkinExecutor = null, ILogger logger = null)
        {
            return new TestEngine(
                gherkinExecutor ?? CreateGherkinExecutor(),
                logger ?? CreateLogger());
        }

        static IGherkinExecutor CreateGherkinExecutor()
        {
            return Substitute.For<IGherkinExecutor>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

    }
}
