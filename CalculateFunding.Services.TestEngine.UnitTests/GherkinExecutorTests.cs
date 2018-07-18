using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.TestRunner.Interfaces;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.UnitTests
{
    [TestClass]
    public class GherkinExecutorTests
    {
        [TestMethod]
        public async Task Execute_WhenGherkinParseResultIsInCacheButNoStepActions_DoesnNotCallParser()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult();

            IEnumerable<ProviderSourceDatasetCurrent> datasets = new[]
            {
                new ProviderSourceDatasetCurrent()
            };

            IEnumerable<TestScenario> testScenarios = new[]
            {
                new TestScenario
                {
                    Id = "scenario-1"
                }
            };

            BuildProject buildProject = new BuildProject();

            IGherkinParser gherkinParser = CreateGherkinParser();

            GherkinParseResult gherkinParseResult = new GherkinParseResult();

            string cacheKey = $"{CacheKeys.GherkinParseResult}scenario-1";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<GherkinParseResult>(Arg.Is(cacheKey), Arg.Any<JsonSerializerSettings>())
                .Returns(gherkinParseResult);

            GherkinExecutor gherkinExecutor = CreateGherkinExecutor(gherkinParser, cacheProvider);

            //Act
            IEnumerable<ScenarioResult> scenarioResults = await gherkinExecutor.Execute(providerResult, datasets, testScenarios, buildProject);

            //Assert
            await
                gherkinParser
                    .DidNotReceive()
                    .Parse(Arg.Any<string>(), Arg.Any<BuildProject>());
        }


        [TestMethod]
        public async Task Execute_WhenGherkinParseResultIsInCacheWithStepActionButAborted_DoesnNotCallParserDoesNotAddDependencies()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult();

            IEnumerable<ProviderSourceDatasetCurrent> datasets = new[]
            {
                new ProviderSourceDatasetCurrent()
            };

            IEnumerable<TestScenario> testScenarios = new[]
            {
                new TestScenario
                {
                    Id = "scenario-1"
                }
            };

            BuildProject buildProject = new BuildProject();

            IGherkinParser gherkinParser = CreateGherkinParser();

            GherkinParseResult stepActionherkinParseResult = new GherkinParseResult { Abort = true };
            

            IStepAction stepAction = Substitute.For<IStepAction>();
            stepAction
                .Execute(Arg.Is(providerResult), Arg.Is(datasets))
                .Returns(stepActionherkinParseResult);

            GherkinParseResult gherkinParseResult = new GherkinParseResult();
            gherkinParseResult
                .StepActions
                .Add(stepAction);

            string cacheKey = $"{CacheKeys.GherkinParseResult}scenario-1";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<GherkinParseResult>(Arg.Is(cacheKey), Arg.Any<JsonSerializerSettings>())
                .Returns(gherkinParseResult);

            GherkinExecutor gherkinExecutor = CreateGherkinExecutor(gherkinParser, cacheProvider);

            //Act
            IEnumerable<ScenarioResult> scenarioResults = await gherkinExecutor.Execute(providerResult, datasets, testScenarios, buildProject);

            //Assert
            await
                gherkinParser
                    .DidNotReceive()
                    .Parse(Arg.Any<string>(), Arg.Any<BuildProject>());

            scenarioResults
                .Count()
                .Should()
                .Be(1);

            scenarioResults
                .First()
                .StepsExecuted
                .Should()
                .Be(0);
        }

        [TestMethod]
        public async Task Execute_WhenGherkinParseResultIsInCacheWithStepActionAndResultHasDependencies_DoesnNotCallParserCreatesResult()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult();

            IEnumerable<ProviderSourceDatasetCurrent> datasets = new[]
            {
                new ProviderSourceDatasetCurrent()
            };

            IEnumerable<TestScenario> testScenarios = new[]
            {
                new TestScenario
                {
                    Id = "scenario-1"
                }
            };

            BuildProject buildProject = new BuildProject();

            IGherkinParser gherkinParser = CreateGherkinParser();

            GherkinParseResult stepActionherkinParseResult = new GherkinParseResult();
            stepActionherkinParseResult
                .Dependencies
                .Add(new Dependency("ds1", "f1", "value"));


            IStepAction stepAction = Substitute.For<IStepAction>();
            stepAction
                .Execute(Arg.Is(providerResult), Arg.Is(datasets))
                .Returns(stepActionherkinParseResult);

            GherkinParseResult gherkinParseResult = new GherkinParseResult();
            gherkinParseResult
                .StepActions
                .Add(stepAction);

            string cacheKey = $"{CacheKeys.GherkinParseResult}scenario-1";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<GherkinParseResult>(Arg.Is(cacheKey), Arg.Any<JsonSerializerSettings>())
                .Returns(gherkinParseResult);

            GherkinExecutor gherkinExecutor = CreateGherkinExecutor(gherkinParser, cacheProvider);

            //Act
            IEnumerable<ScenarioResult> scenarioResults = await gherkinExecutor.Execute(providerResult, datasets, testScenarios, buildProject);

            //Assert
            await
                gherkinParser
                    .DidNotReceive()
                    .Parse(Arg.Any<string>(), Arg.Any<BuildProject>());

            scenarioResults
                .Count()
                .Should()
                .Be(1);

            scenarioResults
                .First()
                .Dependencies
                .Count()
                .Should()
                .Be(1);

            scenarioResults
                .First()
                .Scenario
                .Id
                .Should()
                .Be("scenario-1");

            scenarioResults
                .First()
                .HasErrors
                .Should()
                .BeFalse();

            scenarioResults
               .First()
               .StepsExecuted
               .Should()
               .Be(1);
        }

        [TestMethod]
        public async Task Execute_WhenGherkinParseResultIsInCacheWithStepActionAndResultHasErrorsDoesnNotCallParserCreatesResultWithErrors()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult();

            IEnumerable<ProviderSourceDatasetCurrent> datasets = new[]
            {
                new ProviderSourceDatasetCurrent()
            };

            IEnumerable<TestScenario> testScenarios = new[]
            {
                new TestScenario
                {
                    Id = "scenario-1"
                }
            };

            BuildProject buildProject = new BuildProject();

            IGherkinParser gherkinParser = CreateGherkinParser();

            GherkinParseResult stepActionherkinParseResult = new GherkinParseResult("An error");
            stepActionherkinParseResult
                .Dependencies
                .Add(new Dependency("ds1", "f1", "value"));


            IStepAction stepAction = Substitute.For<IStepAction>();
            stepAction
                .Execute(Arg.Is(providerResult), Arg.Is(datasets))
                .Returns(stepActionherkinParseResult);

            GherkinParseResult gherkinParseResult = new GherkinParseResult();
            gherkinParseResult
                .StepActions
                .Add(stepAction);

            string cacheKey = $"{CacheKeys.GherkinParseResult}scenario-1";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<GherkinParseResult>(Arg.Is(cacheKey), Arg.Any<JsonSerializerSettings>())
                .Returns(gherkinParseResult);

            GherkinExecutor gherkinExecutor = CreateGherkinExecutor(gherkinParser, cacheProvider);

            //Act
            IEnumerable<ScenarioResult> scenarioResults = await gherkinExecutor.Execute(providerResult, datasets, testScenarios, buildProject);

            //Assert
            await
                gherkinParser
                    .DidNotReceive()
                    .Parse(Arg.Any<string>(), Arg.Any<BuildProject>());

            scenarioResults
                .Count()
                .Should()
                .Be(1);

            scenarioResults
                .First()
                .Dependencies
                .Count()
                .Should()
                .Be(1);

            scenarioResults
                .First()
                .Scenario
                .Id
                .Should()
                .Be("scenario-1");

            scenarioResults
                .First()
                .HasErrors
                .Should()
                .BeTrue();
        }

        [TestMethod]
        public async Task Execute_WhenGherkinParseResultIsInCacheWithTeoStepActionAndResultHasErrorsDoesnNotCallParserCreatesResultWithErrors()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult();

            IEnumerable<ProviderSourceDatasetCurrent> datasets = new[]
            {
                new ProviderSourceDatasetCurrent()
            };

            IEnumerable<TestScenario> testScenarios = new[]
            {
                new TestScenario
                {
                    Id = "scenario-1"
                }
            };

            BuildProject buildProject = new BuildProject();

            IGherkinParser gherkinParser = CreateGherkinParser();

            GherkinParseResult stepActionherkinParseResult1 = new GherkinParseResult("An error");
            stepActionherkinParseResult1
                .Dependencies
                .Add(new Dependency("ds1", "f1", "value"));

            GherkinParseResult stepActionherkinParseResult2 = new GherkinParseResult();
            stepActionherkinParseResult2
                .Dependencies
                .Add(new Dependency("ds1", "f1", "value"));


            IStepAction stepAction1 = Substitute.For<IStepAction>();
            stepAction1
                .Execute(Arg.Is(providerResult), Arg.Is(datasets))
                .Returns(stepActionherkinParseResult1);

            IStepAction stepAction2 = Substitute.For<IStepAction>();
            stepAction2
                .Execute(Arg.Is(providerResult), Arg.Is(datasets))
                .Returns(stepActionherkinParseResult2);

            GherkinParseResult gherkinParseResult = new GherkinParseResult();
            gherkinParseResult
                .StepActions
                .AddRange(new[] { stepAction1, stepAction2 });

            string cacheKey = $"{CacheKeys.GherkinParseResult}scenario-1";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<GherkinParseResult>(Arg.Is(cacheKey), Arg.Any<JsonSerializerSettings>())
                .Returns(gherkinParseResult);

            GherkinExecutor gherkinExecutor = CreateGherkinExecutor(gherkinParser, cacheProvider);

            //Act
            IEnumerable<ScenarioResult> scenarioResults = await gherkinExecutor.Execute(providerResult, datasets, testScenarios, buildProject);

            //Assert
            await
                gherkinParser
                    .DidNotReceive()
                    .Parse(Arg.Any<string>(), Arg.Any<BuildProject>());

            scenarioResults
                .Count()
                .Should()
                .Be(1);

            scenarioResults
                .First()
                .Dependencies
                .Count()
                .Should()
                .Be(2);

            scenarioResults
                .First()
                .HasErrors
                .Should()
                .BeTrue();

            scenarioResults
                .First()
                .StepsExecuted
                .Should()
                .Be(2);
        }

        [TestMethod]
        public async Task Execute_WhenGherkinParseResultIsNotInCacheWithTeoStepActionAndResultHasErrorrCreatesResultWithErrors()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult();

            IEnumerable<ProviderSourceDatasetCurrent> datasets = new[]
            {
                new ProviderSourceDatasetCurrent()
            };

            IEnumerable<TestScenario> testScenarios = new[]
            {
                new TestScenario
                {
                    Id = "scenario-1",
                    Current = new TestScenarioVersion
                    {
                        Gherkin = "gherkin"
                    }
                }
            };

            BuildProject buildProject = new BuildProject();

            GherkinParseResult stepActionherkinParseResult1 = new GherkinParseResult("An error");
            stepActionherkinParseResult1
                .Dependencies
                .Add(new Dependency("ds1", "f1", "value"));

            GherkinParseResult stepActionherkinParseResult2 = new GherkinParseResult();
            stepActionherkinParseResult2
                .Dependencies
                .Add(new Dependency("ds1", "f1", "value"));


            IStepAction stepAction1 = Substitute.For<IStepAction>();
            stepAction1
                .Execute(Arg.Is(providerResult), Arg.Is(datasets))
                .Returns(stepActionherkinParseResult1);

            IStepAction stepAction2 = Substitute.For<IStepAction>();
            stepAction2
                .Execute(Arg.Is(providerResult), Arg.Is(datasets))
                .Returns(stepActionherkinParseResult2);

            GherkinParseResult gherkinParseResult = new GherkinParseResult();
            gherkinParseResult
                .StepActions
                .AddRange(new[] { stepAction1, stepAction2 });

            IGherkinParser gherkinParser = CreateGherkinParser();
            gherkinParser
                .Parse(Arg.Is("gherkin"), Arg.Is(buildProject))
                .Returns(gherkinParseResult);

            string cacheKey = $"{CacheKeys.GherkinParseResult}scenario-1";

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<GherkinParseResult>(Arg.Is(cacheKey), Arg.Any<JsonSerializerSettings>())
                .Returns((GherkinParseResult)null);

            GherkinExecutor gherkinExecutor = CreateGherkinExecutor(gherkinParser, cacheProvider);

            //Act
            IEnumerable<ScenarioResult> scenarioResults = await gherkinExecutor.Execute(providerResult, datasets, testScenarios, buildProject);

            //Assert
            scenarioResults
                .Count()
                .Should()
                .Be(1);

            scenarioResults
                .First()
                .Dependencies
                .Count()
                .Should()
                .Be(2);

            scenarioResults
                .First()
                .HasErrors
                .Should()
                .BeTrue();

            scenarioResults
                .First()
                .StepsExecuted
                .Should()
                .Be(2);
        }

        public static GherkinExecutor CreateGherkinExecutor(IGherkinParser gherkinParser = null, ICacheProvider cacheProvider = null)
        {
            return new GherkinExecutor(
                gherkinParser ?? CreateGherkinParser(),
                cacheProvider ?? CreateCacheProvider(),
                CreateResilliencePolicies());
        }

        public static IGherkinParser CreateGherkinParser()
        {
            return Substitute.For<IGherkinParser>();
        }

        public static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        public static ITestRunnerResiliencePolicies CreateResilliencePolicies()
        {
            return TestRunnerResilienceTestHelper.GenerateTestPolicies();
        }
    }
}
