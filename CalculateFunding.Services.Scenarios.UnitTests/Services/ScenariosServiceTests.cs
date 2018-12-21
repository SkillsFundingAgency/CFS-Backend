using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Scenarios.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Scenarios.Services
{
    [TestClass]
    public class ScenariosServiceTests
    {
        const string specificationId = "spec-id";
        const string name = "scenario name";
        const string description = "scenario description";
        const string scenario = "scenario";
        const string scenarioid = "scenario-id";

        [TestMethod]
        async public Task SaveVersion_GivenNullScenarioVersion_ReturnsBadRequestObject()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(logger: logger);

            //Act
            IActionResult result = await service.SaveVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("A null scenario version was provided"));

        }

        [TestMethod]
        async public Task SaveVersion_GivenInvalidModel_ReturnsBadRequestObject()
        {
            //Arrange
            CreateNewTestScenarioVersion model = new CreateNewTestScenarioVersion();
            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "any error")
                });

            IValidator<CreateNewTestScenarioVersion> validator = CreateValidator(validationResult);

            ScenariosService service = CreateScenariosService(logger: logger, createNewTestScenarioVersionValidator: validator);

            //Act
            IActionResult result = await service.SaveVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

        }

        [TestMethod]
        async public Task SaveVersion_GivenNoScenarioIdAndSpecificationDoesNotExist_ReturnsPreConditionFailed()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns((SpecificationSummary)null);

            ScenariosService service = CreateScenariosService(logger: logger, specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.SaveVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(412);

            logger
                .Received(1)
                .Error(Arg.Is($"Unable to find a specification for specification id : {specificationId}"));

        }

        [TestMethod]
        async public Task SaveVersion_GivenNoScenarioIdButSavingCausesInternalServerError_ReturnsInternalServerError()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.InternalServerError);

            ScenariosService service = CreateScenariosService(
                logger: logger,
                specificationsRepository: specificationsRepository,
                scenariosRepository: scenariosRepository);

            //Act
            IActionResult result = await service.SaveVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>();

            StatusCodeResult statusCodeResult = result as StatusCodeResult;

            statusCodeResult
                .StatusCode
                .Should()
                .Be(500);

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to save test scenario with status code: InternalServerError"));
        }

        [TestMethod]
        async public Task SaveVersion_GivenNoScenarioIdAndSavesScenario_UpdateSearchReturnsOK()
        {
            // Arrange
            CreateNewTestScenarioVersion model = CreateModel();

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ScenariosService service = CreateScenariosService(logger: logger,
                specificationsRepository: specificationsRepository, scenariosRepository: scenariosRepository,
                searchRepository: searchrepository);

            scenariosRepository
                .GetCurrentTestScenarioById(Arg.Any<string>())
                .Returns(new CurrentTestScenario());

            // Act
            IActionResult result = await service.SaveVersion(request);

            // Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeAssignableTo<CurrentTestScenario>();

            await
                 searchrepository
                 .Received(1)
                 .Index(Arg.Is<IList<ScenarioIndex>>(m => !string.IsNullOrWhiteSpace(m.First().Id) &&
                                 m.First().Description == description &&
                                 m.First().Name == name &&
                                 m.First().SpecificationId == specificationId &&
                                 m.First().FundingPeriodId == specification.FundingPeriod.Id &&
                                 m.First().FundingPeriodName == specification.FundingPeriod.Name &&
                                 m.First().FundingStreamIds.First() == specification.FundingStreams.First().Id &&
                                 m.First().FundingStreamNames.First() == specification.FundingStreams.First().Name &&
                                 m.First().Status == "Draft" &&
                                 m.First().LastUpdatedDate.HasValue &&
                                 m.First().LastUpdatedDate.Value.Date == DateTime.Now.Date));

            await scenariosRepository
            .Received(1)
            .GetCurrentTestScenarioById(Arg.Any<string>());

            await scenariosRepository
                .Received(1)
                .SaveTestScenario(Arg.Any<TestScenario>());
        }

        [TestMethod]
        async public Task SaveVersion_GivenScenarioIdAndSavesScenario_UpdateSearchReturnsOK()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();
            model.Id = scenarioid;

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            TestScenario testScenario = new TestScenario
            {
                Id = scenarioid,
                SpecificationId = specificationId,
                Name = name,
                Current = new TestScenarioVersion()
                {
                    Description = description,
                    FundingStreamIds = specification.FundingStreams.Select(s => s.Id),
                    FundingPeriodId = specification.FundingPeriod.Id,
                }
            };

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioid))
                .Returns(testScenario);

            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationSummaryById(Arg.Is(specificationId))
                .Returns(specification);

            TestScenarioVersion testScenarioVersion = testScenario.Current.Clone() as TestScenarioVersion;

            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            ScenariosService service = CreateScenariosService(logger: logger,
                scenariosRepository: scenariosRepository,
                searchRepository: searchrepository,
                specificationsRepository: specificationsRepository,
                versionRepository: versionRepository);

            //Act
            IActionResult result = await service.SaveVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await
                 searchrepository
                 .Received(1)
                 .Index(Arg.Is<IList<ScenarioIndex>>(m => !string.IsNullOrWhiteSpace(m.First().Id) &&
                                 m.First().Description == description &&
                                 m.First().Name == name &&
                                 m.First().SpecificationId == specificationId &&
                                 m.First().FundingPeriodId == specification.FundingPeriod.Id &&
                                 m.First().FundingPeriodName == specification.FundingPeriod.Name &&
                                 m.First().FundingStreamIds.First() == specification.FundingStreams.First().Id &&
                                 m.First().FundingStreamNames.First() == specification.FundingStreams.First().Name &&
                                 m.First().Status == "Draft" &&
                                 m.First().LastUpdatedDate.HasValue &&
                                 m.First().LastUpdatedDate.Value.Date == DateTime.Now.Date));

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(testScenarioVersion));
        }

        [TestMethod]
        async public Task SaveVersion_GivenScenarioAndGherkinIsUnchanged_DoesNotCreateNewVersion()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();
            model.Id = scenarioid;

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            TestScenarioVersion testScenarioVersion = new TestScenarioVersion
            {
                Gherkin = "scenario",
                Description = description,
                FundingStreamIds = specification.FundingStreams.Select(s => s.Id),
                FundingPeriodId = specification.FundingPeriod.Id,
            };

            TestScenario testScenario = new TestScenario
            {
                Id = scenarioid,
                SpecificationId = specificationId,
                Name = name,
                Current = testScenarioVersion
            };

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioid))
                .Returns(testScenario);

            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationSummaryById(Arg.Is(specification.Id))
                .Returns(specification);

            ScenariosService service = CreateScenariosService(logger: logger,
                scenariosRepository: scenariosRepository,
                searchRepository: searchrepository,
                specificationsRepository: specificationsRepository);

            //Act
            IActionResult result = await service.SaveVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        async public Task SaveVersion_GivenScenarioAndGherkinIsChanged_ThenCreatesNewVersion()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();
            model.Id = scenarioid;
            model.Scenario = "updated gherkin";

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            TestScenarioVersion testScenarioVersion = new TestScenarioVersion
            {
                Gherkin = "scenario",
                Description = description,
                FundingStreamIds = specification.FundingStreams.Select(s => s.Id),
                FundingPeriodId = specification.FundingPeriod.Id,
            };

            TestScenario testScenario = new TestScenario
            {
                Id = scenarioid,
                SpecificationId = specificationId,
                Name = name,
                Current = testScenarioVersion
            };

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioid))
                .Returns(testScenario);

            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationSummaryById(Arg.Is(specification.Id))
                .Returns(specification);

            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            ScenariosService service = CreateScenariosService(logger: logger,
                scenariosRepository: scenariosRepository,
                searchRepository: searchrepository,
                specificationsRepository: specificationsRepository,
                versionRepository: versionRepository);

            //Act
            IActionResult result = await service.SaveVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await scenariosRepository
                .Received(1)
                .GetCurrentTestScenarioById(Arg.Is(scenarioid));

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(testScenarioVersion));
        }

        [TestMethod]
        async public Task SaveVersion_GivenScenarioAndDescriptionIsChanged_ThenCreatesNewVersion()
        {
            //Arrange
            CreateNewTestScenarioVersion model = CreateModel();
            model.Id = scenarioid;
            model.Description = "updated description";

            string json = JsonConvert.SerializeObject(model);
            byte[] byteArray = Encoding.UTF8.GetBytes(json);
            MemoryStream stream = new MemoryStream(byteArray);

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Body
                .Returns(stream);

            ILogger logger = CreateLogger();

            SpecificationSummary specification = new SpecificationSummary
            {
                Id = specificationId,
                FundingStreams = new List<Reference>()
                {
                    new Reference("fs-id", "fs-name"),
                },
                FundingPeriod = new Reference("period-id", "period name")
            };

            TestScenarioVersion testScenarioVersion = new TestScenarioVersion
            {
                Gherkin = "scenario",
                Description = description,
                FundingStreamIds = specification.FundingStreams.Select(s => s.Id),
                FundingPeriodId = specification.FundingPeriod.Id,
            };

            TestScenario testScenario = new TestScenario
            {
                Id = scenarioid,
                SpecificationId = specificationId,
                Name = name,
                Current = testScenarioVersion
            };

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioid))
                .Returns(testScenario);

            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationSummaryById(Arg.Is(specification.Id))
                .Returns(specification);

            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            ScenariosService service = CreateScenariosService(logger: logger,
                scenariosRepository: scenariosRepository,
                searchRepository: searchrepository,
                specificationsRepository: specificationsRepository, versionRepository: versionRepository);

            //Act
            IActionResult result = await service.SaveVersion(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();

            await scenariosRepository
                .Received(1)
                .GetCurrentTestScenarioById(Arg.Is(scenarioid));

            await
              versionRepository
               .Received(1)
               .SaveVersion(Arg.Is(testScenarioVersion));
        }

        [TestMethod]
        public async Task GetTestScenarioById_GivenNoScenarioId_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(logger: logger);

            //Act
            IActionResult result = await service.GetTestScenarioById(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error("No scenario Id was provided to GetTestScenariosById");
        }

        [TestMethod]
        public async Task GetTestScenarioById_GivenScenarioIdButNoScenarioWasFound_ReturnsNotFound()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "scenarioId", new StringValues(scenarioid) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioid))
                .Returns((TestScenario)null);

            ScenariosService service = CreateScenariosService(logger, scenariosRepository);

            //Act
            IActionResult result = await service.GetTestScenarioById(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetTestScenarioById_GivenScenarioIdAndScenarioWasFound_ReturnsOKResult()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "scenarioId", new StringValues(scenarioid) }

            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioid))
                .Returns(new TestScenario());

            ScenariosService service = CreateScenariosService(logger, scenariosRepository);

            //Act
            IActionResult result = await service.GetTestScenarioById(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetCurrentTestScenarioById_GivenNoScenarioId_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();

            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(logger: logger);

            //Act
            IActionResult result = await service.GetCurrentTestScenarioById(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error("No scenario Id was provided to GetCurrentTestScenarioById");
        }

        [TestMethod]
        public async Task GetCurrentTestScenarioById_GivenScenarioIdButNoScenarioWasFound_ReturnsNotFound()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "scenarioId", new StringValues(scenarioid) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetCurrentTestScenarioById(Arg.Is(scenarioid))
                .Returns((CurrentTestScenario)null);

            ScenariosService service = CreateScenariosService(logger, scenariosRepository);

            //Act
            IActionResult result = await service.GetCurrentTestScenarioById(request);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task GetCurrentTestScenarioById_GivenScenarioIdAndScenarioWasFound_ReturnsOKResult()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "scenarioId", new StringValues(scenarioid) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            ILogger logger = CreateLogger();

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetCurrentTestScenarioById(Arg.Is(scenarioid))
                .Returns(new CurrentTestScenario());

            ScenariosService service = CreateScenariosService(logger, scenariosRepository);

            //Act
            IActionResult result = await service.GetCurrentTestScenarioById(request);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public void UpdateScenarioForSpecification_GivenInvalidModel_LogsDoesNotSave()
        {
            //Arrange
            dynamic anyObject = new { something = 1 };

            string json = JsonConvert.SerializeObject(anyObject);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ScenariosService service = CreateScenariosService();

            //Act
            Func<Task> test = async () => await service.UpdateScenarioForSpecification(message);

            //Assert
            test
              .ShouldThrowExactly<InvalidModelException>();
        }

        [TestMethod]
        public async Task UpdateScenarioForSpecification_GivenModelHasNoChanges_LogsAndReturns()
        {
            //Arrange
            SpecificationVersionComparisonModel specificationVersionComparison = new SpecificationVersionComparisonModel
            {
                Current = new SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } },
                Previous = new SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(logger: logger);

            //Act
            await service.UpdateScenarioForSpecification(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is("No changes detected"));
        }

        [TestMethod]
        public async Task UpdateScenarioForSpecification_GivenModelHasChangedFundingPeriodsButCalcculationsCouldNotBeFound_LogsAndReturns()
        {
            //Arrange
            SpecificationVersionComparisonModel specificationVersionComparison = new SpecificationVersionComparisonModel
            {
                Id = specificationId,
                Current = new SpecificationVersion { FundingPeriod = new Reference { Id = "fp2" } },
                Previous = new SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            IScenariosRepository scenarioRepository = CreateScenariosRepository();
            scenarioRepository
                .GetTestScenariosBySpecificationId(Arg.Is(specificationId))
                .Returns((IEnumerable<TestScenario>)null);

            ScenariosService service = CreateScenariosService(logger, scenarioRepository);

            //Act
            await service.UpdateScenarioForSpecification(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"No scenarios found for specification id: {specificationId}"));
        }

        [TestMethod]
        public async Task UpdateScenarioForSpecification_GivenModelHasChangedFundingPeriodsButBuildProjectNotFound_SavesToCosmosAndSearch()
        {
            //Arrange
            SpecificationVersionComparisonModel specificationVersionComparison = new SpecificationVersionComparisonModel()
            {
                Id = specificationId,
                Current = new SpecificationVersion
                {
                    FundingPeriod = new Reference { Id = "fp2" },
                    Name = "any-name",
                    FundingStreams = new[] { new Reference { Id = "fs1" } }
                },
                Previous = new SpecificationVersion { FundingPeriod = new Reference { Id = "fp1" } }
            };

            string json = JsonConvert.SerializeObject(specificationVersionComparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            ILogger logger = CreateLogger();

            IEnumerable<TestScenario> scenarios = new[]
            {
                new TestScenario
                {
                    Id = "scenario-id",
                    Name = "scenario",
                    SpecificationId = specificationId,
                    Current = new TestScenarioVersion
                    {
                        Author = new Reference("user-id", "username"),
                        Date = DateTimeOffset.Now,
                        PublishStatus = PublishStatus.Draft,
                        Gherkin = "source code",
                        Version = 1
                    }
                }
            };

            IScenariosRepository scenarioRepository = CreateScenariosRepository();
            scenarioRepository
                .GetTestScenariosBySpecificationId(Arg.Is(specificationId))
                .Returns(scenarios);

            TestScenarioVersion testScenarioVersion = scenarios.First().Current.Clone() as TestScenarioVersion;
            testScenarioVersion.Version = 2;

            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();
            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            ISearchRepository<ScenarioIndex> searchRepository = CreateSearchRepository();

            ScenariosService service = CreateScenariosService(logger, scenarioRepository, searchRepository: searchRepository, versionRepository: versionRepository);

            //Act
            await service.UpdateScenarioForSpecification(message);

            //Assert
            scenarios
                .First()
                .Current
                .Version
                .Should()
                .Be(2);

            await
                scenarioRepository
                .Received(1)
                .SaveTestScenarios(Arg.Is(scenarios));

            await
               searchRepository
               .Received(1)
               .Index(Arg.Is<List<ScenarioIndex>>(
                   m => m.Count() == 1 &&
                        m.First().Id == scenarios.First().Id &&
                        m.First().Name == scenarios.First().Name &&
                        m.First().Description == scenarios.First().Current.Description &&
                        m.First().SpecificationId == scenarios.First().SpecificationId
                   ));

            await
              versionRepository
               .Received(1)
               .SaveVersions(Arg.Is<IEnumerable<TestScenarioVersion>>(m => m.First() == testScenarioVersion));
        }

        [TestMethod]
        public async Task ScenariosService_UpdateTestScenarioCalculationGherkin_WhenCalculationNameChanges_ThenTestScenariosUpdated()
        {
            // Arrange
            const string initialGherkin = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name' is greater than '1'";
            const string initialGherkin2 = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name Other' is greater than '1'";

            const string expectedChangedGherkin = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name Updated' is greater than '1'";

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ICacheProvider cacheProvider = CreateCacheProvider();
            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();
            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, cacheProvider: cacheProvider, versionRepository: versionRepository);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = "calcId",
                SpecificationId = "specId",
                Previous = new Calculation()
                {
                    Id = "calc1",
                    Name = "Calc Name",
                    CalculationType = CalculationType.Funding,
                    Description = "Test",
                },
                Current = new Calculation()
                {
                    Id = "calc1",
                    Name = "Calc Name Updated",
                    CalculationType = CalculationType.Funding,
                    Description = "Test",
                },
            };

            List<TestScenario> testScenarios = new List<TestScenario>()
            {
                 new TestScenario()
                 {
                    Current = new TestScenarioVersion()
                    {
                        Gherkin = initialGherkin,
                    },
                    Id = "ts1",
                    SpecificationId = comparison.SpecificationId,
                 },
                 new TestScenario()
                 {
                      Current = new TestScenarioVersion()
                      {
                           Gherkin = initialGherkin2,

                      },
                      Id = "ts2",
                      SpecificationId = comparison.SpecificationId,
                 }
            };

            TestScenarioVersion testScenarioVersion = testScenarios.ElementAt(1).Current.Clone() as TestScenarioVersion;
            testScenarioVersion.Gherkin = expectedChangedGherkin;

            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            scenariosRepository
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId))
                .Returns(testScenarios);

            // Act
            int updateCount = await service.UpdateTestScenarioCalculationGherkin(comparison);

            // Assert
            updateCount
                .Should()
                .Be(1);

            await scenariosRepository
                .Received(1)
                .SaveTestScenario(Arg.Is<TestScenario>(s => s.Current.Gherkin == expectedChangedGherkin));

            await scenariosRepository
                .Received(1)
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<List<TestScenario>>(Arg.Is($"{CacheKeys.TestScenarios}{comparison.SpecificationId}"));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<GherkinParseResult>(Arg.Is($"{CacheKeys.GherkinParseResult}{testScenarios[0].Id}"));
        }

        [TestMethod]
        public async Task ScenariosService_UpdateTestScenarioCalculationGherkin_WhenCalculationNameChanges_ThenMultipleTestScenariosUpdated()
        {
            // Arrange
            const string initialGherkin = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name' is greater than '1'";
            const string initialGherkin2 = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name Other' is greater than '1'";
            const string initialGherkin3 = "Given the dataset 'Test DS' field 'Field Name Two' is greater than '0'\r\nThen the result for 'Calc Name' is greater than '1'";

            const string expectedChangedGherkin = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name Updated' is greater than '1'";
            const string expectedChangedGherkin2 = "Given the dataset 'Test DS' field 'Field Name Two' is greater than '0'\r\nThen the result for 'Calc Name Updated' is greater than '1'";

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ICacheProvider cacheProvider = CreateCacheProvider();
            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();

            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, cacheProvider: cacheProvider, versionRepository: versionRepository);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = "calcId",
                SpecificationId = "specId",
                Previous = new Calculation()
                {
                    Id = "calc1",
                    Name = "Calc Name",
                    CalculationType = CalculationType.Funding,
                    Description = "Test",
                },
                Current = new Calculation()
                {
                    Id = "calc1",
                    Name = "Calc Name Updated",
                    CalculationType = CalculationType.Funding,
                    Description = "Test",
                },
            };

            List<TestScenario> testScenarios = new List<TestScenario>()
            {
                 new TestScenario()
                 {
                    Current = new TestScenarioVersion()
                    {
                        Gherkin = initialGherkin,
                    },
                    Id = "ts1",
                    SpecificationId = comparison.SpecificationId,
                 },
                 new TestScenario()
                 {
                      Current = new TestScenarioVersion()
                      {
                           Gherkin = initialGherkin2,

                      },
                       Id = "ts2",
                        SpecificationId = comparison.SpecificationId,
                 },
                 new TestScenario()
                 {
                      Current = new TestScenarioVersion()
                      {
                           Gherkin = initialGherkin3,

                      },
                      Id = "ts3",
                      SpecificationId = comparison.SpecificationId,
                 },
            };

            scenariosRepository
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId))
                .Returns(testScenarios);

            TestScenarioVersion testScenarioVersion1 = testScenarios.ElementAt(0).Current.Clone() as TestScenarioVersion;
            testScenarioVersion1.Gherkin = expectedChangedGherkin;

            TestScenarioVersion testScenarioVersion2 = testScenarios.ElementAt(1).Current.Clone() as TestScenarioVersion;
            testScenarioVersion2.Gherkin = expectedChangedGherkin2;

            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion1, testScenarioVersion2);

            // Act
            int updateCount = await service.UpdateTestScenarioCalculationGherkin(comparison);

            // Assert
            updateCount
                .Should()
                .Be(2);

            await scenariosRepository
                .Received(1)
                .SaveTestScenario(Arg.Is<TestScenario>(s => s.Current.Gherkin == expectedChangedGherkin));

            await scenariosRepository
                .Received(1)
                .SaveTestScenario(Arg.Is<TestScenario>(s => s.Current.Gherkin == expectedChangedGherkin2));

            await scenariosRepository
                .Received(1)
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<List<TestScenario>>(Arg.Is($"{CacheKeys.TestScenarios}{comparison.SpecificationId}"));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<GherkinParseResult>(Arg.Is($"{CacheKeys.GherkinParseResult}{testScenarios[0].Id}"));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<GherkinParseResult>(Arg.Is($"{CacheKeys.GherkinParseResult}{testScenarios[2].Id}"));
        }

        [TestMethod]
        public async Task ScenariosService_UpdateTestScenarioCalculationGherkin_WhenCalculationNameChangesWithDifferentCase_ThenTestScenariosUpdated()
        {
            // Arrange
            const string initialGherkin = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'calc name' is greater than '1'";
            const string initialGherkin2 = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name Other' is greater than '1'";

            const string expectedChangedGherkin = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'Calc Name Updated' is greater than '1'";

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ICacheProvider cacheProvider = CreateCacheProvider();
            IVersionRepository<TestScenarioVersion> versionRepository = CreateVersionRepository();

            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, cacheProvider: cacheProvider, versionRepository: versionRepository);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = "calcId",
                SpecificationId = "specId",
                Previous = new Calculation()
                {
                    Id = "calc1",
                    Name = "Calc Name",
                    CalculationType = CalculationType.Funding,
                    Description = "Test",
                },
                Current = new Calculation()
                {
                    Id = "calc1",
                    Name = "Calc Name Updated",
                    CalculationType = CalculationType.Funding,
                    Description = "Test",
                },
            };

            List<TestScenario> testScenarios = new List<TestScenario>()
            {
                 new TestScenario()
                 {
                      Current = new TestScenarioVersion()
                      {
                           Gherkin = initialGherkin,

                      },
                       Id = "ts1",
                        SpecificationId = comparison.SpecificationId,
                 },
                 new TestScenario()
                 {
                      Current = new TestScenarioVersion()
                      {
                           Gherkin = initialGherkin2,

                      },
                       Id = "ts2",
                        SpecificationId = comparison.SpecificationId,
                 }
            };

            scenariosRepository
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId))
                .Returns(testScenarios);

            TestScenarioVersion testScenarioVersion = testScenarios.ElementAt(1).Current.Clone() as TestScenarioVersion;
            testScenarioVersion.Gherkin = expectedChangedGherkin;

            versionRepository
                .CreateVersion(Arg.Any<TestScenarioVersion>(), Arg.Any<TestScenarioVersion>())
                .Returns(testScenarioVersion);

            // Act
            int updateCount = await service.UpdateTestScenarioCalculationGherkin(comparison);

            // Assert
            updateCount
                .Should()
                .Be(1);

            await scenariosRepository
                .Received(1)
                .SaveTestScenario(Arg.Is<TestScenario>(s => s.Current.Gherkin == expectedChangedGherkin));

            await scenariosRepository
                .Received(1)
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<List<TestScenario>>(Arg.Is($"{CacheKeys.TestScenarios}{comparison.SpecificationId}"));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<GherkinParseResult>(Arg.Is($"{CacheKeys.GherkinParseResult}{testScenarios[0].Id}"));
        }

        [TestMethod]
        public async Task ScenariosService_UpdateTestScenarioCalculationGherkin_WhenTestScenariosDoesNotContainChangedCalculationGherkin_ThenNoTestScenariosUpdated()
        {
            // Arrange
            const string initialGherkin = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'C1' is greater than '1'";
            const string initialGherkin2 = "Given the dataset 'Test DS' field 'Field Name' is greater than '0'\r\nThen the result for 'C2' is greater than '1'";

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ICacheProvider cacheProvider = CreateCacheProvider();
            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, cacheProvider: cacheProvider);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = "calcId",
                SpecificationId = "specId",
                Previous = new Calculation()
                {
                    Id = "calc1",
                    Name = "Calc Name",
                    CalculationType = CalculationType.Funding,
                    Description = "Test",
                },
                Current = new Calculation()
                {
                    Id = "calc1",
                    Name = "Calc Name Updated",
                    CalculationType = CalculationType.Funding,
                    Description = "Test",
                },
            };

            List<TestScenario> testScenarios = new List<TestScenario>()
            {
                 new TestScenario()
                 {
                      Current = new TestScenarioVersion()
                      {
                           Gherkin = initialGherkin,

                      },
                       Id = "ts1",
                        SpecificationId = comparison.SpecificationId,
                 },
                 new TestScenario()
                 {
                      Current = new TestScenarioVersion()
                      {
                           Gherkin = initialGherkin2,

                      },
                       Id = "ts2",
                        SpecificationId = comparison.SpecificationId,
                 }
            };

            scenariosRepository
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId))
                .Returns(testScenarios);

            // Act
            int updateCount = await service.UpdateTestScenarioCalculationGherkin(comparison);

            // Assert
            updateCount
                .Should()
                .Be(0);

            await scenariosRepository
                .Received(0)
                .SaveTestScenario(Arg.Any<TestScenario>());

            await scenariosRepository
                .Received(1)
                .GetTestScenariosBySpecificationId(Arg.Is(comparison.SpecificationId));

            await
                cacheProvider
                .Received(0)
                .RemoveAsync<List<TestScenario>>($"{CacheKeys.TestScenarios}{comparison.SpecificationId}");

            await
                cacheProvider
                .Received(0)
                .RemoveAsync<GherkinParseResult>($"{CacheKeys.GherkinParseResult}{testScenarios[0].Id}");
        }

        [TestMethod]
        public async Task ScenariosService_UpdateTestScenarioCalculationGherkin_WhenCalculationNameNotChanged_ThenNoTestScenariosUpdated()
        {
            // Arrange
            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ICacheProvider cacheProvider = CreateCacheProvider();
            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, cacheProvider: cacheProvider);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = "calcId",
                SpecificationId = "specId",
                Previous = new Calculation()
                {
                    Id = "calc1",
                    Name = "Calc Name",
                    CalculationType = CalculationType.Funding,
                    Description = "Test",
                },
                Current = new Calculation()
                {
                    Id = "calc1",
                    Name = "Calc Name",
                    CalculationType = CalculationType.Funding,
                    Description = "Test",
                },
            };

            // Act
            int updateCount = await service.UpdateTestScenarioCalculationGherkin(comparison);

            // Assert
            updateCount
                .Should()
                .Be(0);

            await scenariosRepository
                .Received(0)
                .SaveTestScenario(Arg.Any<TestScenario>());

            await scenariosRepository
                .Received(0)
                .GetTestScenariosBySpecificationId(Arg.Any<string>());

            await
                cacheProvider
                .Received(0)
                .RemoveAsync<List<TestScenario>>(Arg.Is($"{CacheKeys.TestScenarios}{comparison.SpecificationId}"));

            await
                cacheProvider
                .Received(0)
                .RemoveAsync<GherkinParseResult>(Arg.Any<string>());
        }

        [TestMethod]
        public async Task ScenariosService_UpdateScenarioForCalculation_WhenCalculationNameNotChanged_ThenNoTestScenariosUpdated()
        {
            // Arrange
            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, logger: logger);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                CalculationId = "calcId",
                SpecificationId = "specId",
                Previous = new Calculation()
                {
                    Id = "calc1",
                    Name = "Calc Name",
                    CalculationType = CalculationType.Funding,
                    Description = "Test",
                },
                Current = new Calculation()
                {
                    Id = "calc1",
                    Name = "Calc Name",
                    CalculationType = CalculationType.Funding,
                    Description = "Test",
                },
            };

            string json = JsonConvert.SerializeObject(comparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            // Act
            await service.UpdateScenarioForCalculation(message);

            // Assert
            await scenariosRepository
                .Received(0)
                .SaveTestScenario(Arg.Any<TestScenario>());

            await scenariosRepository
                .Received(0)
                .GetTestScenariosBySpecificationId(Arg.Any<string>());

            logger
                .Received(1)
                .Information("A total of {updateCount} Test Scenarios updated for calculation ID '{calculationId}'", Arg.Is(0), Arg.Is(comparison.CalculationId));
        }

        [TestMethod]
        public void ScenariosService_UpdateScenarioForCalculation_WhenModelIsNull_ThenErrorThrown()
        {
            // Arrange
            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, logger: logger);

            CalculationVersionComparisonModel comparison = null;

            string json = JsonConvert.SerializeObject(comparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            // Act
            Func<Task> action = async () => await service.UpdateScenarioForCalculation(message);

            // Assert
            action
               .ShouldThrowExactly<InvalidModelException>()
               .Which
               .Message
               .Should()
               .Be("The model for type: SpecificationVersionComparisonModel is invalid with the following errors Null or invalid model provided");

            logger
                .Received(1)
                .Error("A null CalculationVersionComparisonModel was provided to UpdateScenarioForCalculation");
        }

        [TestMethod]
        public void ScenariosService_UpdateScenarioForCalculation_WhenCalculationIdIsNull_ThenErrorThrown()
        {
            // Arrange
            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, logger: logger);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                Current = new Calculation(),
                SpecificationId = "specId",
                CalculationId = null,
                Previous = new Calculation(),
            };

            string json = JsonConvert.SerializeObject(comparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            // Act
            Func<Task> action = async () => await service.UpdateScenarioForCalculation(message);

            // Assert
            action
               .ShouldThrowExactly<InvalidModelException>()
               .Which
               .Message
               .Should()
               .Be("The model for type: CalculationVersionComparisonModel is invalid with the following errors Null or invalid calculationId provided");

            logger
                .Received(1)
                .Warning("Null or invalid calculationId provided to UpdateScenarioForCalculation");
        }

        [TestMethod]
        public void ScenariosService_UpdateScenarioForCalculation_WhenSpecificationIdIsNull_ThenErrorThrown()
        {
            // Arrange
            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            ILogger logger = CreateLogger();

            ScenariosService service = CreateScenariosService(scenariosRepository: scenariosRepository, logger: logger);

            CalculationVersionComparisonModel comparison = new CalculationVersionComparisonModel()
            {
                Current = new Calculation(),
                SpecificationId = null,
                CalculationId = "calcId",
                Previous = new Calculation(),
            };

            string json = JsonConvert.SerializeObject(comparison);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            // Act
            Func<Task> action = async () => await service.UpdateScenarioForCalculation(message);

            // Assert
            action
               .ShouldThrowExactly<InvalidModelException>()
               .Which
               .Message
               .Should()
               .Be("The model for type: CalculationVersionComparisonModel is invalid with the following errors Null or invalid SpecificationId provided");

            logger
                .Received(1)
                .Warning("Null or invalid SpecificationId provided to UpdateScenarioForCalculation");
        }

        static ScenariosService CreateScenariosService(ILogger logger = null, IScenariosRepository scenariosRepository = null,
           ISpecificationsRepository specificationsRepository = null, IValidator<CreateNewTestScenarioVersion> createNewTestScenarioVersionValidator = null,
           ISearchRepository<ScenarioIndex> searchRepository = null, ICacheProvider cacheProvider = null, IMessengerService messengerService = null,
           IBuildProjectRepository buildProjectRepository = null, IVersionRepository<TestScenarioVersion> versionRepository = null)
        {
            return new ScenariosService(logger ?? CreateLogger(), scenariosRepository ?? CreateScenariosRepository(), specificationsRepository ?? CreateSpecificationsRepository(),
                createNewTestScenarioVersionValidator ?? CreateValidator(), searchRepository ?? CreateSearchRepository(),
                cacheProvider ?? CreateCacheProvider(), messengerService ?? CreateMessengerService(), buildProjectRepository ?? CreateBuildProjectRepository(),
                versionRepository ?? CreateVersionRepository());
        }

        static IVersionRepository<TestScenarioVersion> CreateVersionRepository()
        {
            return Substitute.For<IVersionRepository<TestScenarioVersion>>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        static IScenariosRepository CreateScenariosRepository()
        {
            return Substitute.For<IScenariosRepository>();
        }

        static IBuildProjectRepository CreateBuildProjectRepository()
        {
            return Substitute.For<IBuildProjectRepository>();
        }

        static ISpecificationsRepository CreateSpecificationsRepository()
        {
            return Substitute.For<ISpecificationsRepository>();
        }

        static IValidator<CreateNewTestScenarioVersion> CreateValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<CreateNewTestScenarioVersion> validator = Substitute.For<IValidator<CreateNewTestScenarioVersion>>();

            validator
               .ValidateAsync(Arg.Any<CreateNewTestScenarioVersion>())
               .Returns(validationResult);

            return validator;
        }

        static ISearchRepository<ScenarioIndex> CreateSearchRepository()
        {
            return Substitute.For<ISearchRepository<ScenarioIndex>>();
        }

        static CreateNewTestScenarioVersion CreateModel()
        {
            return new CreateNewTestScenarioVersion
            {
                SpecificationId = specificationId,
                Name = name,
                Scenario = scenario,
                Description = description
            };
        }
    }
}
