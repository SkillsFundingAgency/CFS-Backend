using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using NSubstitute;
using Serilog;
using CalculateFunding.Services.Scenarios.Interfaces;
using FluentValidation;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Repositories.Common.Search;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using Newtonsoft.Json;
using System.IO;
using FluentValidation.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models;
using System.Net;
using System.Linq;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Versioning;

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
                SpecificationId =  specificationId,
                Name = name,
                History = new List<TestScenarioVersion>(),
                Current = new TestScenarioVersion()
                {
                    Description = description,
                    FundingStreamIds = specification.FundingStreams.Select(s=>s.Id),
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
                FundingStreamIds = specification.FundingStreams.Select(s=>s.Id),
                FundingPeriodId = specification.FundingPeriod.Id,
            };

            TestScenario testScenario = new TestScenario
            {
                Id = scenarioid,
                SpecificationId = specificationId,
                Name = name,
                History = new List<TestScenarioVersion>
                {
                    testScenarioVersion
                },
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

            testScenario
                .History
                .Count
                .Should()
                .Be(1);
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
                History = new List<TestScenarioVersion>
                {
                    testScenarioVersion
                },
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

            testScenario
                .History
                .Count
                .Should()
                .Be(2);

            await scenariosRepository
                .Received(1)
                .GetCurrentTestScenarioById(Arg.Is(scenarioid));
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
                History = new List<TestScenarioVersion>
                {
                    testScenarioVersion
                },
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

            testScenario
                .History
                .Count
                .Should()
                .Be(2);

            await scenariosRepository
                .Received(1)
                .GetCurrentTestScenarioById(Arg.Is(scenarioid));
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

        static ScenariosService CreateScenariosService(ILogger logger = null, IScenariosRepository scenariosRepository = null,
            ISpecificationsRepository specificationsRepository = null, IValidator<CreateNewTestScenarioVersion> createNewTestScenarioVersionValidator = null,
            ISearchRepository<ScenarioIndex> searchRepository = null, ICacheProvider cacheProvider = null, IMessengerService messengerService = null, IBuildProjectRepository buildProjectRepository = null)
        {
            return new ScenariosService(logger ?? CreateLogger(), scenariosRepository ?? CreateScenariosRepository(), specificationsRepository ?? CreateSpecificationsRepository(),
                createNewTestScenarioVersionValidator ?? CreateValidator(), searchRepository ?? CreateSearchRepository(), 
                cacheProvider ?? CreateCacheProvider(), messengerService ?? CreateMessengerService(), buildProjectRepository ?? CreateBuildProjectRepository());
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
                    History = new List<TestScenarioVersion>(),
                    SpecificationId = specificationId,
                    Current = new TestScenarioVersion
                    {
                        Author = new Reference("user-id", "username"),
                        Date = DateTime.UtcNow,
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

            ISearchRepository<ScenarioIndex> searchRepository = CreateSearchRepository();

            ScenariosService service = CreateScenariosService(logger, scenarioRepository, searchRepository: searchRepository);

            //Act
            await service.UpdateScenarioForSpecification(message);

            //Assert
            scenarios
                .First()
                .Current
                .Version
                .Should()
                .Be(2);

            scenarios
               .First()
               .History
               .Count
               .Should()
               .Be(1);

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
                validationResult = new ValidationResult();

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
