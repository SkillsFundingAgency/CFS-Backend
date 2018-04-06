using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Options;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Compiler.Interfaces;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Mvc;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class BuildProjectsServiceTests
    {
        const string SpecificationId = "bbe8bec3-1395-445f-a190-f7e300a8c336";
        const string BuildProjectId = "47b680fa-4dbe-41e0-a4ce-c25e41a634c1";

        [TestMethod]
        public void UpdateAllocations_GivenNullMessage_ThrowsArgumentNullException()
        {
            //Arrange
            Message message = null;

            BuildProjectsService buildProjectsService = CreateBuildProjectsService();

            //Act
            Func<Task> test = () => buildProjectsService.UpdateAllocations(message);

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void UpdateAllocations_GivenNullPayload_ThrowsArgumentNullException()
        {
            //Arrange
            Message message = new Message(new byte[0]);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService();

            //Act
            Func<Task> test = () => buildProjectsService.UpdateAllocations(message);

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void UpdateAllocations_GivenSpecificationIdKeyyNotFoundOnMessage_ThrowsKeyNotFoundExceptionn()
        {
            //Arrange
            BuildProject payload = new BuildProject();

            var json = JsonConvert.SerializeObject(payload);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            BuildProjectsService buildProjectsService = CreateBuildProjectsService();

            //Act
            Func<Task> test = () => buildProjectsService.UpdateAllocations(message);

            //Assert
            test
                .ShouldThrowExactly<KeyNotFoundException>();
        }

        [TestMethod]
        public void UpdateAllocations_GivenNullOrEmptySpecificationId_ThrowsArgumentNullException()
        {
            //Arrange
            BuildProject payload = new BuildProject();

            var json = JsonConvert.SerializeObject(payload);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
               .UserProperties.Add("specification-id", "");

            BuildProjectsService buildProjectsService = CreateBuildProjectsService();

            //Act
            Func<Task> test = () => buildProjectsService.UpdateAllocations(message);

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void UpdateAllocations_GivenSpecificationCouldNotBeFound_ThrowsArgumentException()
        {
            //Arrange
            BuildProject payload = new BuildProject();
           
            var json = JsonConvert.SerializeObject(payload);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
                .UserProperties.Add("specification-id", SpecificationId);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService();

            //Act
            Func<Task> test = () => buildProjectsService.UpdateAllocations(message);

            //Assert
            test
                .ShouldThrowExactly<ArgumentException>();
        }

        [TestMethod]
        public void UpdateAllocations_GivenBuildProjectWithNoSpecFoundButFailedToUpdateBuildProject_ThrowsException()
        {
            //Arrange
            Specification specification = new Specification();

            BuildProject buildProject = new BuildProject
            {
                Id = BuildProjectId
            };

            var json = JsonConvert.SerializeObject(buildProject);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
               .UserProperties.Add("specification-id", SpecificationId);

            ISpecificationRepository specificationsRepository = CreateSpecificationRepository();
            specificationsRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .UpdateBuildProject(Arg.Is(buildProject))
                .Returns(HttpStatusCode.InternalServerError);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(
                specificationsRepository: specificationsRepository, buildProjectsRepository: buildProjectsRepository);

            //Act
            Func<Task> test = () => buildProjectsService.UpdateAllocations(message);

            //Assert
            test
                .ShouldThrowExactly<Exception>();
        }

        //[TestMethod]
        //public void UpdateAllocations_GivenNoProviderSummariesFound_ThrowsException()
        //{
        //    //Arrange
        //    Specification specification = new Specification();

        //    BuildProject buildProject = new BuildProject
        //    {
        //        Id = BuildProjectId
        //    };

        //    var json = JsonConvert.SerializeObject(buildProject);

        //    Message message = new Message(Encoding.UTF8.GetBytes(json));
        //    message
        //       .UserProperties.Add("specification-id", SpecificationId);

        //    ISpecificationRepository specificationsRepository = CreateSpecificationRepository();
        //    specificationsRepository
        //        .GetSpecificationById(Arg.Is(SpecificationId))
        //        .Returns(specification);

        //    IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
        //    buildProjectsRepository
        //        .UpdateBuildProject(Arg.Any<BuildProject>())
        //        .Returns(HttpStatusCode.OK);

        //    BuildProjectsService buildProjectsService = CreateBuildProjectsService(
        //        specificationsRepository: specificationsRepository, buildProjectsRepository: buildProjectsRepository);

        //    //Act
        //    Func<Task> test = () => buildProjectsService.UpdateAllocations(message);

        //    //Assert
        //    test
        //        .ShouldThrowExactly<Exception>();
        //}

        //[TestMethod]
        //public void UpdateAllocations_GivenSpecificationExistsOnBuildProjectButNoProviderSummaries_ThrowsException()
        //{
        //    //Arrange
        //    SpecificationSummary specification = new SpecificationSummary();

        //    BuildProject buildProject = new BuildProject
        //    {
        //        Id = BuildProjectId,
        //        Specification = specification
        //    };

        //    var json = JsonConvert.SerializeObject(buildProject);

        //    Message message = new Message(Encoding.UTF8.GetBytes(json));
        //    message
        //       .UserProperties.Add("specification-id", SpecificationId);

        //    BuildProjectsService buildProjectsService = CreateBuildProjectsService();

        //    //Act
        //    Func<Task> test = () => buildProjectsService.UpdateAllocations(message);

        //    //Assert
        //    test
        //        .ShouldThrowExactly<Exception>();
        //}

        //[TestMethod]
        //async public Task UpdateAllocation_WhenProviderSummariesFound_CallsCalcEngine()
        //{
        //    //Arrange
        //    SpecificationSummary specification = new SpecificationSummary();

        //    BuildProject buildProject = new BuildProject
        //    {
        //        Id = BuildProjectId,
        //        Specification = specification
        //    };

        //    var json = JsonConvert.SerializeObject(buildProject);

        //    Message message = new Message(Encoding.UTF8.GetBytes(json));
        //    message
        //       .UserProperties.Add("specification-id", SpecificationId);

        //    IEnumerable<ProviderSummary> summaries = new[]
        //    {
        //        new ProviderSummary()
        //    };

        //    IProviderResultsRepository providerResultsRepository = CreateProviderResultsRepository();
        //    providerResultsRepository
        //        .PartitionProviderSummaries(Arg.Is)
        //        .Returns(summaries);

        //    IEnumerable<ProviderResult> results = new[]
        //    {
        //        new ProviderResult()
        //    };

        //    IMessengerService messengerService = CreateMessengerService();

        //    ICalculationEngine calculationEngine = CreateCalculationEngine();
        //    calculationEngine
        //           .GenerateAllocations(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<ProviderSummary>>(), Arg.Any<Func<string, string, Task<IEnumerable<ProviderSourceDataset>>>>())
        //           .Returns(results);

        //    BuildProjectsService buildProjectsService = CreateBuildProjectsService(providerResultsRepository: providerResultsRepository,
        //        messengerService: messengerService, calculationEngine: calculationEngine);

        //    //Act
        //    await buildProjectsService.UpdateAllocations(message);

        //    //Assert
        //    await
        //        messengerService
        //            .Received(1)
        //            .SendAsync(Arg.Is("calc-events-generate-allocations-results"), Arg.Is(buildProject), Arg.Any<Dictionary<string, string>>());
        //}

        [TestMethod]
        public void UpdateBuildProjectRelationships_GivenNullMessage_ThrowsArgumentNullException()
        {
            //Arrange
            Message message = null;

            BuildProjectsService buildProjectsService = CreateBuildProjectsService();

            //Act
            Func<Task> test = () => buildProjectsService.UpdateBuildProjectRelationships(message);

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void UpdateBuildProjectRelationships_GivenPayload_ThrowsArgumentNullException()
        {
            //Arrange
            Message message = new Message(new byte[0]);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService();

            //Act
            Func<Task> test = () => buildProjectsService.UpdateBuildProjectRelationships(message);

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void UpdateBuildProjectRelationships_GivenSpecificationIdKeyyNotFoundOnMessage_ThrowsKeyNotFoundExceptionn()
        {
            //Arrange
            DatasetRelationshipSummary payload = new DatasetRelationshipSummary();

            var json = JsonConvert.SerializeObject(payload);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            BuildProjectsService buildProjectsService = CreateBuildProjectsService();

            //Act
            Func<Task> test = () => buildProjectsService.UpdateBuildProjectRelationships(message);

            //Assert
            test
                .ShouldThrowExactly<KeyNotFoundException>();
        }

        [TestMethod]
        public void UpdateBuildProjectRelationships_GivenNullOrEmptySpecificationId_ThrowsArgumentNullException()
        {
            //Arrange
            DatasetRelationshipSummary payload = new DatasetRelationshipSummary();

            var json = JsonConvert.SerializeObject(payload);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
               .UserProperties.Add("specification-id", "");

            BuildProjectsService buildProjectsService = CreateBuildProjectsService();

            //Act
            Func<Task> test = () => buildProjectsService.UpdateBuildProjectRelationships(message);

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void UpdateBuildProjectRelationships_GivenBuildProjectNotFound_ThrowsException()
        {
            //Arrange
            DatasetRelationshipSummary payload = new DatasetRelationshipSummary();

            var json = JsonConvert.SerializeObject(payload);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
               .UserProperties.Add("specification-id", SpecificationId);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns((BuildProject)null);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService();

            //Act
            Func<Task> test = () => buildProjectsService.UpdateBuildProjectRelationships(message);

            //Assert
            test
                .ShouldThrowExactly<Exception>();
        }

        [TestMethod]
        public void UpdateBuildProjectRelationships_GivenBuildProjectWasFoundBuyWithoutABuild_ThrowsException()
        {
            //Arrange
            DatasetRelationshipSummary payload = new DatasetRelationshipSummary();

            var json = JsonConvert.SerializeObject(payload);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
               .UserProperties.Add("specification-id", SpecificationId);

            BuildProject buildProject = new BuildProject();

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository: buildProjectsRepository);

            //Act
            Func<Task> test = () => buildProjectsService.UpdateBuildProjectRelationships(message);

            //Assert
            test
                .ShouldThrowExactly<Exception>();
        }

        [TestMethod]
        async public Task UpdateBuildProjectRelationships_GivenRelationshipNameAlreadyExists_DoesNotCompileAndUpdate()
        {
            //Arrange
            const string relationshipName = "test--name";

            DatasetRelationshipSummary payload = new DatasetRelationshipSummary
            {
                Name = relationshipName
            };

            var json = JsonConvert.SerializeObject(payload);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
               .UserProperties.Add("specification-id", SpecificationId);

            BuildProject buildProject = new BuildProject
            {
                Id = BuildProjectId,
                Build = new Build(),
                Specification = new SpecificationSummary { Id = SpecificationId },
                DatasetRelationships = new List<DatasetRelationshipSummary>
                {
                    new DatasetRelationshipSummary{ Name = relationshipName }
                }
            };

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            ICompilerFactory compilerFactory = CreateCompilerfactory();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository: buildProjectsRepository, compilerFactory: compilerFactory);

            //Act
            await buildProjectsService.UpdateBuildProjectRelationships(message);

            //Assert
            await
                buildProjectsRepository
                .DidNotReceive()
                .UpdateBuildProject(Arg.Any<BuildProject>());

            compilerFactory
                .DidNotReceive()
                .GetCompiler(Arg.Any<IEnumerable<SourceFile>>());
        }

        [TestMethod]
        async public Task UpdateBuildProjectRelationships_GivenRelationship_CompilesAndUpdates()
        {
            //Arrange
            const string relationshipName = "test--name";

            DatasetRelationshipSummary payload = new DatasetRelationshipSummary
            {
                Name = relationshipName
            };

            var json = JsonConvert.SerializeObject(payload);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
               .UserProperties.Add("specification-id", SpecificationId);

            BuildProject buildProject = new BuildProject
            {
                Id = BuildProjectId,
                Build = new Build(),
                Specification = new SpecificationSummary { Id = SpecificationId }
            };

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            buildProjectsRepository
                 .UpdateBuildProject(Arg.Is(buildProject))
                 .Returns(HttpStatusCode.OK);

            ICompilerFactory compilerFactory = CreateCompilerfactory();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository: buildProjectsRepository, compilerFactory: compilerFactory);

            //Act
            await buildProjectsService.UpdateBuildProjectRelationships(message);

            //Assert
            await
                buildProjectsRepository
                .Received(1)
                .UpdateBuildProject(Arg.Any<BuildProject>());

            compilerFactory
                .Received(1)
                .GetCompiler(Arg.Any<IEnumerable<SourceFile>>());
        }

        [TestMethod]
        public void UpdateBuildProjectRelationships_GivenRelationshipButFailsToUpdate_ThrowsException()
        {
            //Arrange
            const string relationshipName = "test--name";

            DatasetRelationshipSummary payload = new DatasetRelationshipSummary
            {
                Name = relationshipName
            };

            var json = JsonConvert.SerializeObject(payload);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
               .UserProperties.Add("specification-id", SpecificationId);

            BuildProject buildProject = new BuildProject
            {
                Id = BuildProjectId,
                Build = new Build(),
                Specification = new SpecificationSummary { Id = SpecificationId }
            };

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            buildProjectsRepository
                .UpdateBuildProject(Arg.Is(buildProject))
                .Returns(HttpStatusCode.InternalServerError);

            ICompilerFactory compilerFactory = CreateCompilerfactory();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository: buildProjectsRepository, compilerFactory: compilerFactory);

            //Act
            Func<Task> test = () => buildProjectsService.UpdateBuildProjectRelationships(message);

            //Assert
            test
                .ShouldThrowExactly<Exception>();
        }

        [TestMethod]
        async public Task GetBuildProjectBySpecificationId_GivenNoSpecificationId_ReturnsBadRequest()
        {
            //Arrange
            HttpRequest request = Substitute.For<HttpRequest>();
           
            ILogger logger = CreateLogger();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(logger: logger);

            //Act
            IActionResult result = await buildProjectsService.GetBuildProjectBySpecificationId(request);

            //Assert
            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to GetBuildProjectBySpecificationId"));
        }

        [TestMethod]
        async public Task GetBuildProjectBySpecificationId_GivenButBuildProjectNotFound_ReturnsNotFound()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns((BuildProject)null);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository);

            //Act
            IActionResult result = await buildProjectsService.GetBuildProjectBySpecificationId(request);

            //Assert
            result
                .Should()
                .BeAssignableTo<NotFoundResult>();
        }

        [TestMethod]
        async public Task GetBuildProjectBySpecificationId_GivenButBuildProjectFound_ReturnsOKResult()
        {
            //Arrange
            IQueryCollection queryStringValues = new QueryCollection(new Dictionary<string, StringValues>
            {
                { "specificationId", new StringValues(SpecificationId) }
            });

            HttpRequest request = Substitute.For<HttpRequest>();
            request
                .Query
                .Returns(queryStringValues);

            BuildProject buildProject = new BuildProject
            {
                Build = new Build()
            };

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository);

            //Act
            IActionResult result = await buildProjectsService.GetBuildProjectBySpecificationId(request);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>();
        }

        static BuildProjectsService CreateBuildProjectsService(IBuildProjectsRepository buildProjectsRepository = null, IMessengerService messengerService = null,
            ServiceBusSettings EventHubSettings = null, ILogger logger = null, ITelemetry telemetry = null,
            Interfaces.IProviderResultsRepository providerResultsRepository = null, ISpecificationRepository specificationsRepository = null, ISourceFileGeneratorProvider sourceFileGeneratorProvider = null,
            ICompilerFactory compilerFactory = null)
        {
            return new BuildProjectsService(buildProjectsRepository ?? CreateBuildProjectsRepository(), messengerService ?? CreateMessengerService(),
                EventHubSettings ?? CreateEventHubSettings(), logger ?? CreateLogger(), telemetry ?? CreateTelemetry(),
                providerResultsRepository ?? CreateProviderResultsRepository(), specificationsRepository ?? CreateSpecificationRepository(),
                sourceFileGeneratorProvider ?? CreateSourceFileGeneratorProvider(), compilerFactory ?? CreateCompilerfactory());
        }

        static Message CreateMessage(string specificationId = SpecificationId)
        {

            dynamic anyObject = new { specificationId };

            string json = JsonConvert.SerializeObject(anyObject);

            return new Message(Encoding.UTF8.GetBytes(json));
        }

        static ISourceFileGeneratorProvider CreateSourceFileGeneratorProvider()
        {
            return Substitute.For<ISourceFileGeneratorProvider>();
        }

        static ICompilerFactory CreateCompilerfactory()
        {
            return Substitute.For<ICompilerFactory>();
        }

        static ITelemetry CreateTelemetry()
        {
            return Substitute.For<ITelemetry>();
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        static ServiceBusSettings CreateEventHubSettings()
        {
            return new ServiceBusSettings
            {
            };
        }

        static IBuildProjectsRepository CreateBuildProjectsRepository()
        {
            return Substitute.For<IBuildProjectsRepository>();
        }

        static ICalculationEngine CreateCalculationEngine()
        {
            return Substitute.For<ICalculationEngine>();
        }

        static Interfaces.IProviderResultsRepository CreateProviderResultsRepository()
        {
            return Substitute.For<Interfaces.IProviderResultsRepository>();
        }

        static ISpecificationRepository CreateSpecificationRepository()
        {
            return Substitute.For<ISpecificationRepository>();
        }
    }
}
