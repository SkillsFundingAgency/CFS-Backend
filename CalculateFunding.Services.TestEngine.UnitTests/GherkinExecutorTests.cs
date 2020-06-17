using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Common.Caching;
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
using CalculateFunding.Services.TestRunner.Services;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.TestEngine.Interfaces;
using Serilog;
using CalculateFunding.Models.Code;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.ProviderLegacy;

namespace CalculateFunding.Services.TestRunner.UnitTests
{
    [TestClass]
    public class GherkinExecutorTests
    {
        [TestMethod]
        public async Task Execute_WhenGherkinParseResultIsInCacheButNoStepActions_DoesNotCallParser()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult();

            IEnumerable<ProviderSourceDataset> datasets = new[]
            {
                new ProviderSourceDataset()
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
                    .Parse(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<BuildProject>());
        }

        [TestMethod]
        public async Task Execute_WhenGherkinParseResultIsInCacheWithStepActionButAborted_DoesNotCallParserDoesNotAddDependencies()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult();

            IEnumerable<ProviderSourceDataset> datasets = new[]
            {
                new ProviderSourceDataset()
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
                    .Parse(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<BuildProject>());

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
        public async Task Execute_WhenGherkinParseResultIsInCacheWithStepActionAndResultHasDependencies_DoesNotCallParserCreatesResult()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult();

            IEnumerable<ProviderSourceDataset> datasets = new[]
            {
                new ProviderSourceDataset()
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
                    .Parse(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<BuildProject>());

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
        public async Task Execute_WhenGherkinParseResultIsInCacheWithStepActionAndResultHasErrors_DoesNotCallParserCreatesResultWithErrors()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult();

            IEnumerable<ProviderSourceDataset> datasets = new[]
            {
                new ProviderSourceDataset()
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
                    .Parse(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<BuildProject>());

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
        public async Task Execute_WhenGherkinParseResultIsInCacheWithTeoStepActionAndResultHasErrors_DoesNotCallParserCreatesResultWithErrors()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult();

            IEnumerable<ProviderSourceDataset> datasets = new[]
            {
                new ProviderSourceDataset()
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
                    .Parse(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<BuildProject>());

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
        public async Task Execute_WhenGherkinParseResultIsNotInCacheWithTeoStepActionAndResultHasError_CreatesResultWithErrors()
        {
            //Arrange
            ProviderResult providerResult = new ProviderResult();

            IEnumerable<ProviderSourceDataset> datasets = new[]
            {
                new ProviderSourceDataset()
            };

            IEnumerable<TestScenario> testScenarios = new[]
            {
                new TestScenario
                {
                    Id = "scenario-1",
                    Current = new TestScenarioVersion
                    {
                        Gherkin = "gherkin"
                    },
                    SpecificationId = "spec1"
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
                .Parse(Arg.Is("spec1"), Arg.Is("gherkin"), Arg.Is(buildProject))
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

        [TestMethod]
        public async Task Execute_WhenCalculationNameDatasetAndFieldNameCaseAreAllPerfectMatches_ThenTestIsSuccessfullyExecuted()
        {
            // Arrange
            string dataSetName = "Test Dataset";
            string fieldName = "URN";
            string calcName = "Test Calc";
            string gherkin = $"Given the dataset '{dataSetName}' field '{fieldName}' is equal to '100050'\n\nThen the result for '{calcName}' is greater than '12' ";

            ICodeMetadataGeneratorService codeMetadataGeneratorService = CreateCodeMetadataGeneratorService();
            codeMetadataGeneratorService
                .GetTypeInformation(Arg.Any<byte[]>())
                .Returns(new List<TypeInformation>
                {
                    new TypeInformation { Type = "Calculations", Methods = new List<MethodInformation> { new MethodInformation { FriendlyName = calcName } } },
                    new TypeInformation { Type = "Datasets", Properties = new List<PropertyInformation> { new PropertyInformation { FriendlyName = dataSetName, Type = "DSType" } }},
                    new TypeInformation { Type = "DSType", Properties = new List<PropertyInformation> { new PropertyInformation { FriendlyName = fieldName, Type = "String" } }}
                });

            IProviderResultsRepository providerResultsRepository = CreateProviderResultsRepository();

            ITestRunnerResiliencePolicies resiliencePolicies = CreateResiliencePolicies();

            IStepParserFactory stepParserFactory = new StepParserFactory(codeMetadataGeneratorService, providerResultsRepository, resiliencePolicies);

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetAssemblyBySpecificationId(Arg.Is("spec1"))
                .Returns(new byte[1]);

            ILogger logger = CreateLogger();

            GherkinParser gherkinParser = new GherkinParser(stepParserFactory, calculationsRepository, logger);

            ICacheProvider cacheProvider = CreateCacheProvider();
            
            GherkinExecutor gherkinExecutor = CreateGherkinExecutor(gherkinParser, cacheProvider);

            ProviderResult providerResult = new ProviderResult
            {
                Provider = new ProviderSummary { Id = "prov1" },
                CalculationResults = new List<CalculationResult>
                {
                    new CalculationResult { Calculation = new Common.Models.Reference { Name = calcName }, Value = (decimal)14 }
                }
            };
            IEnumerable<ProviderSourceDataset> datasets = new List<ProviderSourceDataset>
            {
                new ProviderSourceDataset
                {
                    DataRelationship = new Common.Models.Reference { Name = dataSetName },
                    Current = new ProviderSourceDatasetVersion
                    {
                        Rows = new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { fieldName, 100050 } }
                        }
                    }
                }
            };
            IEnumerable<TestScenario> testScenarios = new List<TestScenario>
            {
                new TestScenario { Id = "ts1", Name = "Test Scenario 1", SpecificationId = "spec1", Current = new TestScenarioVersion { Gherkin = gherkin } }
            };
            BuildProject buildProject = new BuildProject { Build = new Build() };

            // Act
            IEnumerable<ScenarioResult> scenarioResults = await gherkinExecutor.Execute(providerResult, datasets, testScenarios, buildProject);

            // Assert
            scenarioResults
                .Should()
                .HaveCount(1);

            scenarioResults
                .First().HasErrors
                .Should()
                .BeFalse("there should be no errors");

            scenarioResults
                .First().StepsExecuted
                .Should()
                .Be(scenarioResults.First().TotalSteps, "all steps should be executed");
        }

        [TestMethod]
        public async Task Execute_WhenCalculationNameCaseIsDifferent_ThenTestIsSuccessfullyExecuted()
        {
            // Arrange
            string dataSetName = "Test Dataset";
            string fieldName = "URN";
            string calcName = "Test Calc";
            string gherkin = $"Given the dataset '{dataSetName}' field '{fieldName}' is equal to '100050'\n\nThen the result for '{calcName.ToLower()}' is greater than '12' ";

            ICodeMetadataGeneratorService codeMetadataGeneratorService = CreateCodeMetadataGeneratorService();
            codeMetadataGeneratorService
                .GetTypeInformation(Arg.Any<byte[]>())
                .Returns(new List<TypeInformation>
                {
                    new TypeInformation { Type = "Calculations", Methods = new List<MethodInformation> { new MethodInformation { FriendlyName = calcName } } },
                    new TypeInformation { Type = "Datasets", Properties = new List<PropertyInformation> { new PropertyInformation { FriendlyName = dataSetName, Type = "DSType" } }},
                    new TypeInformation { Type = "DSType", Properties = new List<PropertyInformation> { new PropertyInformation { FriendlyName = fieldName, Type = "String" } }}
                });

            IProviderResultsRepository providerResultsRepository = CreateProviderResultsRepository();

            ITestRunnerResiliencePolicies resiliencePolicies = CreateResiliencePolicies();

            IStepParserFactory stepParserFactory = new StepParserFactory(codeMetadataGeneratorService, providerResultsRepository, resiliencePolicies);

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetAssemblyBySpecificationId(Arg.Is("spec1"))
                .Returns(new byte[1]);

            ILogger logger = CreateLogger();

            GherkinParser gherkinParser = new GherkinParser(stepParserFactory, calculationsRepository, logger);

            ICacheProvider cacheProvider = CreateCacheProvider();

            GherkinExecutor gherkinExecutor = CreateGherkinExecutor(gherkinParser, cacheProvider);

            ProviderResult providerResult = new ProviderResult
            {
                Provider = new ProviderSummary { Id = "prov1" },
                CalculationResults = new List<CalculationResult>
                {
                    new CalculationResult { Calculation = new Common.Models.Reference { Name = calcName }, Value = (decimal) 14 }
                }
            };
            IEnumerable<ProviderSourceDataset> datasets = new List<ProviderSourceDataset>
            {
                new ProviderSourceDataset
                {
                    DataRelationship = new Common.Models.Reference { Name = dataSetName },
                    Current = new ProviderSourceDatasetVersion
                    {
                        Rows = new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { fieldName, 100050 } }
                        }
                    }
                }
            };
            IEnumerable<TestScenario> testScenarios = new List<TestScenario>
            {
                new TestScenario { Id = "ts1", Name = "Test Scenario 1", SpecificationId = "spec1", Current = new TestScenarioVersion { Gherkin = gherkin } }
            };
            BuildProject buildProject = new BuildProject { Build = new Build() };

            // Act
            IEnumerable<ScenarioResult> scenarioResults = await gherkinExecutor.Execute(providerResult, datasets, testScenarios, buildProject);

            // Assert
            scenarioResults
                .Should()
                .HaveCount(1);

            scenarioResults
                .First().HasErrors
                .Should()
                .BeFalse("there should be no errors");

            scenarioResults
                .First().StepsExecuted
                .Should()
                .Be(scenarioResults.First().TotalSteps, "all steps should be executed");
        }

        [TestMethod]
        public async Task Execute_WhenDatasetNameCaseIsDifferent_ThenTestIsSuccessfullyExecuted()
        {
            // Arrange
            string dataSetName = "Test Dataset";
            string fieldName = "URN";
            string calcName = "Test Calc";
            string gherkin = $"Given the dataset '{dataSetName.ToLower()}' field '{fieldName}' is equal to '100050'\n\nThen the result for '{calcName}' is greater than '12' ";

            ICodeMetadataGeneratorService codeMetadataGeneratorService = CreateCodeMetadataGeneratorService();
            codeMetadataGeneratorService
                .GetTypeInformation(Arg.Any<byte[]>())
                .Returns(new List<TypeInformation>
                {
                    new TypeInformation { Type = "Calculations", Methods = new List<MethodInformation> { new MethodInformation { FriendlyName = calcName } } },
                    new TypeInformation { Type = "Datasets", Properties = new List<PropertyInformation> { new PropertyInformation { FriendlyName = dataSetName, Type = "DSType" } }},
                    new TypeInformation { Type = "DSType", Properties = new List<PropertyInformation> { new PropertyInformation { FriendlyName = fieldName, Type = "String" } }}
                });

            IProviderResultsRepository providerResultsRepository = CreateProviderResultsRepository();

            ITestRunnerResiliencePolicies resiliencePolicies = CreateResiliencePolicies();

            IStepParserFactory stepParserFactory = new StepParserFactory(codeMetadataGeneratorService, providerResultsRepository, resiliencePolicies);

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetAssemblyBySpecificationId(Arg.Is("spec1"))
                .Returns(new byte[1]);

            ILogger logger = CreateLogger();

            GherkinParser gherkinParser = new GherkinParser(stepParserFactory, calculationsRepository, logger);

            ICacheProvider cacheProvider = CreateCacheProvider();

            GherkinExecutor gherkinExecutor = CreateGherkinExecutor(gherkinParser, cacheProvider);

            ProviderResult providerResult = new ProviderResult
            {
                Provider = new ProviderSummary { Id = "prov1" },
                CalculationResults = new List<CalculationResult>
                {
                    new CalculationResult { Calculation = new Common.Models.Reference { Name = calcName }, Value = (decimal)14 }
                }
            };
            IEnumerable<ProviderSourceDataset> datasets = new List<ProviderSourceDataset>
            {
                new ProviderSourceDataset
                {
                    DataRelationship = new Common.Models.Reference { Name = dataSetName },
                    Current = new ProviderSourceDatasetVersion
                    {
                        Rows = new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { fieldName, 100050 } }
                        }
                    }
                }
            };
            IEnumerable<TestScenario> testScenarios = new List<TestScenario>
            {
                new TestScenario { Id = "ts1", Name = "Test Scenario 1", SpecificationId = "spec1", Current = new TestScenarioVersion { Gherkin = gherkin } }
            };
            BuildProject buildProject = new BuildProject { Build = new Build() };

            // Act
            IEnumerable<ScenarioResult> scenarioResults = await gherkinExecutor.Execute(providerResult, datasets, testScenarios, buildProject);

            // Assert
            scenarioResults
                .Should()
                .HaveCount(1);

            scenarioResults
                .First().HasErrors
                .Should()
                .BeFalse("there should be no errors");

            scenarioResults
                .First().StepsExecuted
                .Should()
                .Be(scenarioResults.First().TotalSteps, "all steps should be executed");
        }

        [TestMethod]
        public async Task Execute_WhenFieldNameCaseIsDifferent_ThenTestIsSuccessfullyExecuted()
        {
            // Arrange
            string dataSetName = "Test Dataset";
            string fieldName = "URN";
            string calcName = "Test Calc";
            string gherkin = $"Given the dataset '{dataSetName}' field '{fieldName.ToLower()}' is equal to '100050'\n\nThen the result for '{calcName}' is greater than '12' ";

            ICodeMetadataGeneratorService codeMetadataGeneratorService = CreateCodeMetadataGeneratorService();
            codeMetadataGeneratorService
                .GetTypeInformation(Arg.Any<byte[]>())
                .Returns(new List<TypeInformation>
                {
                    new TypeInformation { Type = "Calculations", Methods = new List<MethodInformation> { new MethodInformation { FriendlyName = calcName } } },
                    new TypeInformation { Type = "Datasets", Properties = new List<PropertyInformation> { new PropertyInformation { FriendlyName = dataSetName, Type = "DSType" } }},
                    new TypeInformation { Type = "DSType", Properties = new List<PropertyInformation> { new PropertyInformation { FriendlyName = fieldName, Type = "String" } }}
                });

            IProviderResultsRepository providerResultsRepository = CreateProviderResultsRepository();

            ITestRunnerResiliencePolicies resiliencePolicies = CreateResiliencePolicies();

            IStepParserFactory stepParserFactory = new StepParserFactory(codeMetadataGeneratorService, providerResultsRepository, resiliencePolicies);

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetAssemblyBySpecificationId(Arg.Is("spec1"))
                .Returns(new byte[1]);

            ILogger logger = CreateLogger();

            GherkinParser gherkinParser = new GherkinParser(stepParserFactory, calculationsRepository, logger);

            ICacheProvider cacheProvider = CreateCacheProvider();

            GherkinExecutor gherkinExecutor = CreateGherkinExecutor(gherkinParser, cacheProvider);

            ProviderResult providerResult = new ProviderResult
            {
                Provider = new ProviderSummary { Id = "prov1" },
                CalculationResults = new List<CalculationResult>
                {
                    new CalculationResult { Calculation = new Common.Models.Reference { Name = calcName }, Value = (decimal)14 }
                }
            };
            IEnumerable<ProviderSourceDataset> datasets = new List<ProviderSourceDataset>
            {
                new ProviderSourceDataset
                {
                    DataRelationship = new Common.Models.Reference { Name = dataSetName },
                    Current = new ProviderSourceDatasetVersion
                    {
                        Rows = new List<Dictionary<string, object>>
                        {
                            new Dictionary<string, object> { { fieldName, 100050 } }
                        }
                    }
                }
            };
            IEnumerable<TestScenario> testScenarios = new List<TestScenario>
            {
                new TestScenario { Id = "ts1", Name = "Test Scenario 1", SpecificationId = "spec1", Current = new TestScenarioVersion { Gherkin = gherkin } }
            };
            BuildProject buildProject = new BuildProject { Build = new Build() };

            // Act
            IEnumerable<ScenarioResult> scenarioResults = await gherkinExecutor.Execute(providerResult, datasets, testScenarios, buildProject);

            // Assert
            scenarioResults
                .Should()
                .HaveCount(1);

            scenarioResults
                .First().HasErrors
                .Should()
                .BeFalse("there should be no errors");

            scenarioResults
                .First().StepsExecuted
                .Should()
                .Be(scenarioResults.First().TotalSteps, "all steps should be executed");
        }

        private static GherkinExecutor CreateGherkinExecutor(IGherkinParser gherkinParser = null, ICacheProvider cacheProvider = null, ITestRunnerResiliencePolicies resiliencePolicies = null)
        {
            return new GherkinExecutor(
                gherkinParser ?? CreateGherkinParser(),
                cacheProvider ?? CreateCacheProvider(),
                resiliencePolicies ?? CreateResiliencePolicies());
        }

        private static IGherkinParser CreateGherkinParser()
        {
            return Substitute.For<IGherkinParser>();
        }

        private static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        private static ITestRunnerResiliencePolicies CreateResiliencePolicies()
        {
            return TestRunnerResilienceTestHelper.GenerateTestPolicies();
        }

        private static ICodeMetadataGeneratorService CreateCodeMetadataGeneratorService()
        {
            return Substitute.For<ICodeMetadataGeneratorService>();
        }

        private static IProviderResultsRepository CreateProviderResultsRepository()
        {
            return Substitute.For<IProviderResultsRepository>();
        }

        private static ICalculationsRepository CreateCalculationsRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }
    }
}
