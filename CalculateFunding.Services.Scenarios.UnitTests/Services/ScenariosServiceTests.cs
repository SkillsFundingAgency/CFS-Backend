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
        async public Task SaveVersionAsync_GivenNullScenarioVersion_ReturnsBadRequestObject()
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
        async public Task SaveVersionAsync_GivenInvalidModel_ReturnsBadRequestObject()
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
        async public Task SaveVersionAsync_GivenNoScenarioIdAndSpecificationDoesNotExist_ReturnsPreConditionFailed()
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
                .GetSpecificationById(Arg.Is(specificationId))
                .Returns((Specification)null);

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
        async public Task SaveVersionAsync_GivenNoScenarioIdButSavingCausesInternalServerError_ReturnsInternalServerError()
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

            Specification specification = new Specification
            {
                Id = specificationId,
                FundingStream = new Reference("fs-id", "fs-name"),
                AcademicYear = new Reference("period-id", "period name")
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.InternalServerError);

            ScenariosService service = CreateScenariosService(logger: logger, 
                specificationsRepository: specificationsRepository, scenariosRepository: scenariosRepository);

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
        async public Task SaveVersionAsync_GivenNoScenarioIdAndSavesScenario_UpdateSearchReturnsOK()
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

            Specification specification = new Specification
            {
                Id = specificationId,
                FundingStream = new Reference("fs-id", "fs-name"),
                AcademicYear = new Reference("period-id", "period name")
            };

            ISpecificationsRepository specificationsRepository = CreateSpecificationsRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(specificationId))
                .Returns(specification);

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ScenariosService service = CreateScenariosService(logger: logger,
                specificationsRepository: specificationsRepository, scenariosRepository: scenariosRepository,
                searchRepository: searchrepository);

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
                                m.First().PeriodId == specification.AcademicYear.Id &&
                                m.First().PeriodName == specification.AcademicYear.Name &&
                                m.First().FundingStreamId == specification.FundingStream.Id &&
                                m.First().FundingStreamName == specification.FundingStream.Name &&
                                m.First().Status == "Draft" &&
                                m.First().LastUpdatedDate.HasValue && 
                                m.First().LastUpdatedDate.Value.Date == DateTime.Now.Date));
                
        }

        [TestMethod]
        async public Task SaveVersionAsync_GivenScenarioIdAndSavesScenario_UpdateSearchReturnsOK()
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

            Specification specification = new Specification
            {
                Id = specificationId,
                FundingStream = new Reference("fs-id", "fs-name"),
                AcademicYear = new Reference("period-id", "period name")
            };

            TestScenario testScenario = new TestScenario
            {
                Id = scenarioid,
                Specification = new SpecificationSummary
                {
                    FundingStream = specification.FundingStream,
                    Period = specification.AcademicYear,
                    Id = specificationId,
                    Name = "spec name"
                },
                Name = name,
                Description = description,
                FundingStream = specification.FundingStream,
                Period = specification.AcademicYear,
                History = new List<TestScenarioVersion>(),
                Current = new TestScenarioVersion()
            };

            IScenariosRepository scenariosRepository = CreateScenariosRepository();
            scenariosRepository
                .GetTestScenarioById(Arg.Is(scenarioid))
                .Returns(testScenario);

            scenariosRepository
                .SaveTestScenario(Arg.Any<TestScenario>())
                .Returns(HttpStatusCode.OK);

            ISearchRepository<ScenarioIndex> searchrepository = CreateSearchRepository();

            ScenariosService service = CreateScenariosService(logger: logger,
                scenariosRepository: scenariosRepository,
                searchRepository: searchrepository);

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
                                 m.First().PeriodId == specification.AcademicYear.Id &&
                                 m.First().PeriodName == specification.AcademicYear.Name &&
                                 m.First().FundingStreamId == specification.FundingStream.Id &&
                                 m.First().FundingStreamName == specification.FundingStream.Name &&
                                 m.First().Status == "Draft" &&
                                 m.First().LastUpdatedDate.HasValue &&
                                 m.First().LastUpdatedDate.Value.Date == DateTime.Now.Date));

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

        static ScenariosService CreateScenariosService(ILogger logger = null, IScenariosRepository scenariosRepository = null,
            ISpecificationsRepository specificationsRepository = null, IValidator<CreateNewTestScenarioVersion> createNewTestScenarioVersionValidator = null,
            ISearchRepository<ScenarioIndex> searchRepository = null, ICacheProvider cacheProvider = null, IMessengerService messengerService = null, IBuildProjectRepository buildProjectRepository = null)
        {
            return new ScenariosService(logger ?? CreateLogger(), scenariosRepository ?? CreateScenariosRepository(), specificationsRepository ?? CreateSpecificationsRepository(),
                createNewTestScenarioVersionValidator ?? CreateValidator(), searchRepository ?? CreateSearchRepository(), 
                cacheProvider ?? CreateCacheProvider(), messengerService ?? CreateMessengerService(), buildProjectRepository ?? CreateBuildProjectRepository());
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
