using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class BuildProjectsServiceTests
    {
        const string SpecificationId = "bbe8bec3-1395-445f-a190-f7e300a8c336";
        const string BuildProjectId = "47b680fa-4dbe-41e0-a4ce-c25e41a634c1";

        [TestMethod]
        public void GenerateAllocations_GivenNullPayload_ThrowsArgumentNullException()
        {
            //Arrange
            Message message = new Message();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService();

            //Act
            Func<Task> test = async () => await buildProjectsService.GenerateAllocationsInstruction(message);

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void GenerateAllocations_GivenPayloadButNullOrEmptySpecificationId_ThrowsArgumentNullException()
        {
            //Arrange
            Message message = CreateMessage("");

            BuildProjectsService buildProjectsService = CreateBuildProjectsService();

            //Act
            Func<Task> test = async () => await buildProjectsService.GenerateAllocationsInstruction(message);

            //Assert
            test
                .ShouldThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        async public Task GenerateAllocations_GivenBuildProjectCouldNotBeFound_LogsAndReturns()
        {
            //Arrange
            Message message = CreateMessage();

            ILogger logger = CreateLogger();

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns((BuildProject)null);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository, logger: logger, specificationsRepository: specificationRepository);

            //Act
            await buildProjectsService.GenerateAllocationsInstruction(message);

            //Assert
            logger
                .Received(1)
                .Error($"Failed to find build project for specification id: {SpecificationId}");

            await
                specificationRepository
                    .DidNotReceive()
                    .GetSpecificationById(Arg.Any<string>());
        }

        [TestMethod]
        async public Task GenerateAllocations_GivenBuildProjectFoundButNoBuild_LogsAndReturns()
        {
            //Arrange
            Message message = CreateMessage();

            ILogger logger = CreateLogger();

            BuildProject buildProject = new BuildProject
            {
                Id = BuildProjectId,
                Specification = new SpecificationSummary
                {
                    Id = SpecificationId
                }
            };

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository, logger: logger, specificationsRepository: specificationRepository);

            //Act
            await buildProjectsService.GenerateAllocationsInstruction(message);

            //Assert
            logger
                .Received(1)
                .Error($"Failed to find build project assembly for build project id: {BuildProjectId}");

            await
                specificationRepository
                    .DidNotReceive()
                    .GetSpecificationById(Arg.Any<string>());
        }

        [TestMethod]
        async public Task GenerateAllocations_GivenBuildProjectFoundButNoBuildAssembly_LogsAndReturns()
        {
            //Arrange
            Message message = CreateMessage();

            ILogger logger = CreateLogger();

            BuildProject buildProject = new BuildProject
            {
                Id = BuildProjectId,
                Specification = new SpecificationSummary
                {
                    Id = SpecificationId
                },
                Build = new Build()
            };

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository, logger: logger, specificationsRepository: specificationRepository);

            //Act
            await buildProjectsService.GenerateAllocationsInstruction(message);

            //Assert
            logger
                .Received(1)
                .Error($"Failed to find build project assembly for build project id: {BuildProjectId}");

            await
                specificationRepository
                    .DidNotReceive()
                    .GetSpecificationById(Arg.Any<string>());
        }

        [TestMethod]
        async public Task GenerateAllocations_GivenBuildProjectFoundButSpecificationNotFound_LogsAndReturns()
        {
            //Arrange
            Message message = CreateMessage();

            ILogger logger = CreateLogger();

            BuildProject buildProject = new BuildProject
            {
                Id = BuildProjectId,
                Specification = new SpecificationSummary
                {
                    Id = SpecificationId
                },
                Build = new Build { AssemblyBase64 = "123456789" }
            };

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns((Specification)null);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository, logger: logger, specificationsRepository: specificationRepository);

            //Act
            await buildProjectsService.GenerateAllocationsInstruction(message);

            //Assert
            logger
                .Received(1)
                .Error($"Failed to find specification for specification id: {SpecificationId}");

            await
                buildProjectsRepository
                    .DidNotReceive()
                    .UpdateBuildProject(Arg.Any<BuildProject>());
        }

        [TestMethod]
        async public Task GenerateAllocations_GivenBuildProjectAndSpecButFailsToUpdateBuildProject_LogsAndReturns()
        {
            //Arrange
            Message message = CreateMessage();

            ILogger logger = CreateLogger();

            BuildProject buildProject = new BuildProject
            {
                Id = BuildProjectId,
                Specification = new SpecificationSummary
                {
                    Id = SpecificationId
                },
                Build = new Build { AssemblyBase64 = "123456789" }
            };

            Specification specification = new Specification
            {
                Id = SpecificationId,
                Name = "Any name"
            };

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            buildProjectsRepository
                .UpdateBuildProject(Arg.Is(buildProject))
                .Returns(HttpStatusCode.InternalServerError);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationById(Arg.Is(SpecificationId))
                .Returns(specification);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository, logger: logger, specificationsRepository: specificationRepository);

            //Act
            await buildProjectsService.GenerateAllocationsInstruction(message);

            //Assert
            logger
                .Received(1)
                .Error($"Failed to find update build project with build project id: {buildProject.Id} with status code: InternalServerError");
        }

        static BuildProjectsService CreateBuildProjectsService(IBuildProjectsRepository buildProjectsRepository = null, IMessengerService messengerService = null,
            ServiceBusSettings serviceBusSettings = null, ILogger logger = null, ICalculationEngine calculationEngine = null,
            IProviderResultsRepository providerResultsRepository = null, ISpecificationRepository specificationsRepository = null)
        {
            return new BuildProjectsService(buildProjectsRepository ?? CreateBuildProjectsRepository(), messengerService ?? CreateMessengerService(),
                serviceBusSettings ?? CreateServiceBusSettings(), logger ?? CreateLogger(), calculationEngine ?? CreateCalculationEngine(),
                providerResultsRepository ?? CreateProviderResultsRepository(), specificationsRepository ?? CreateSpecificationRepository());
        }

        static Message CreateMessage(string specificationId = SpecificationId)
        {
            Message message = new Message();

            dynamic anyObject = new { specificationId };

            string json = JsonConvert.SerializeObject(anyObject);

            message.Body = Encoding.UTF8.GetBytes(json);

            return message;
        }

        static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        static ServiceBusSettings CreateServiceBusSettings()
        {
            return new ServiceBusSettings
            {
                CalcsServiceBusTopicName = "calcs-events"
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

        static IProviderResultsRepository CreateProviderResultsRepository()
        {
            return Substitute.For<IProviderResultsRepository>();
        }

        static ISpecificationRepository CreateSpecificationRepository()
        {
            return Substitute.For<ISpecificationRepository>();
        }
    }
}
