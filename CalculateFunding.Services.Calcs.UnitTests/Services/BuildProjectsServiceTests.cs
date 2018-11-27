using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
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
using CalculateFunding.Services.Core.Interfaces.Caching;
using System.Linq;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Languages;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Models.Jobs;

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
                .Should().ThrowExactly<ArgumentNullException>();
        }

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
                .Should().ThrowExactly<ArgumentNullException>();
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
                .Should().ThrowExactly<ArgumentNullException>();
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
                .Should().ThrowExactly<KeyNotFoundException>();
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
                .Should().ThrowExactly<ArgumentNullException>();
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
                .Should().ThrowExactly<Exception>();
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
                .Should().ThrowExactly<Exception>();
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
                SpecificationId = SpecificationId,
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
        public async Task UpdateBuildProjectRelationships_GivenRelationship_CompilesAndUpdates()
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
                SpecificationId = SpecificationId,
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
                SpecificationId = SpecificationId,
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
                .Should().ThrowExactly<Exception>();
        }

        [TestMethod]
        public void UpdateBuildProjectRelationships_GivenBuildProjectNotFoundAndCouldNotFindASpecSummary_ThrowsException()
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

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns((BuildProject)null);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(SpecificationId))
                .Returns((SpecificationSummary)null);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository, specificationsRepository: specificationRepository);

            //Act
            Func<Task> test = () => buildProjectsService.UpdateBuildProjectRelationships(message);

            //Assert
            test
                .Should().ThrowExactly<Exception>();
        }

        [TestMethod]
        public void UpdateBuildProjectRelationships_GivenBuildProjectNotFoundAndCreateFails_ThrowsException()
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

            SpecificationSummary specification = null;

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns((BuildProject)null);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(SpecificationId))
                .Returns(specification);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository, specificationsRepository: specificationRepository);

            //Act
            Func<Task> test = () => buildProjectsService.UpdateBuildProjectRelationships(message);

            //Assert
            test
                .Should().ThrowExactly<Exception>();
        }

        [TestMethod]
        public async Task UpdateBuildProjectRelationships_GivenBuildProjectNotFound_ThenItIsCreated()
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

            SpecificationSummary specification = new SpecificationSummary
            {
                Id = SpecificationId,
                Name = "spec-name"
            };

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns((BuildProject)null);

            buildProjectsRepository
                .UpdateBuildProject(Arg.Any<BuildProject>())
                .Returns(HttpStatusCode.OK);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(SpecificationId))
                .Returns(specification);

            BuildProject createdBuildProject = new BuildProject()
            {
                Build = new Build()
                {

                }
            };

            ICalculationService calculationService = CreateCalculationService();

            calculationService
                .CreateBuildProject(Arg.Is(SpecificationId), Arg.Any<IEnumerable<Models.Calcs.Calculation>>())
                .Returns(createdBuildProject);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository, specificationsRepository: specificationRepository, calculationService: calculationService);

            // Act
            await buildProjectsService.UpdateBuildProjectRelationships(message);

            // Assert
            await buildProjectsRepository
                .Received(1)
                .UpdateBuildProject(Arg.Any<BuildProject>());

            await calculationService
                 .Received(1)
                 .CreateBuildProject(Arg.Is(SpecificationId), Arg.Any<IEnumerable<Models.Calcs.Calculation>>());
        }

        [TestMethod]
        public async Task UpdateBuildProjectRelationships_GivenBuildProjectNotAndCreatesNewBuildProject_CompilesAndUpdates()
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

            SpecificationSummary specification = new SpecificationSummary
            {
                Id = SpecificationId
            };

            BuildProject buildProject = new BuildProject
            {
                Id = BuildProjectId,
                Build = new Build(),
                SpecificationId = SpecificationId,
            };

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns((BuildProject)null);

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();
            specificationRepository
                .GetSpecificationSummaryById(Arg.Is(SpecificationId))
                .Returns(specification);

            ICompilerFactory compilerFactory = CreateCompilerfactory();

            ICalculationService calculationService = CreateCalculationService();
            calculationService
                .CreateBuildProject(Arg.Any<string>(), Arg.Is(Enumerable.Empty<Models.Calcs.Calculation>()))
                .Returns(buildProject);

            buildProjectsRepository
               .UpdateBuildProject(Arg.Is(buildProject))
               .Returns(HttpStatusCode.OK);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository,
                specificationsRepository: specificationRepository, compilerFactory: compilerFactory, calculationService: calculationService);

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
        public async Task GetBuildProjectBySpecificationId_GivenNoSpecificationId_ReturnsBadRequest()
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
        public async Task GetBuildProjectBySpecificationId_GivenButBuildProjectNotFound_ReturnsNotFound()
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
        public async Task GetBuildProjectBySpecificationId_GivenButBuildProjectFound_ReturnsOKResult()
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

        [TestMethod]
        public void CompileBuildProject_WhenBuildingBasicCalculation_ThenCompilesOk()
        {
            // Arrange
            string specificationId = "test-spec1";
            List<Models.Calcs.Calculation> calculations = new List<Models.Calcs.Calculation>
            {
                new Models.Calcs.Calculation
                {
                    Id = "calcId1",
                    Name = "calc 1",
                    Description = "test calc",
                    AllocationLine = new Models.Reference { Id = "alloc1", Name = "alloc one" },
                    CalculationSpecification = new Models.Reference{ Id = "calcSpec1", Name = "calc spec 1" },
                    Policies = new List<Models.Reference>
                    {
                        new Models.Reference{ Id = "policy1", Name="policy one"}
                    },
                    Current = new CalculationVersion
                    {
                         SourceCode = "return 10"
                    }
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository.GetCalculationsBySpecificationId(Arg.Is(specificationId)).Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository.UpdateBuildProject(Arg.Any<BuildProject>()).Returns(HttpStatusCode.OK);

            BuildProjectsService buildProjectsService = CreateBuildProjectsServiceWithRealCompiler(buildProjectsRepository, calculationsRepository: calculationsRepository);

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            // Act
            Func<Task> action = async () => await buildProjectsService.CompileBuildProject(buildProject);

            // Assert
            action.Should().NotThrow();
            buildProject.Build.Success.Should().BeTrue();
        }

        [TestMethod]
        public void CompileBuildProject_WhenBuildingCalculationWithMinimumDetail_ThenCompilesOk()
        {
            // Arrange
            string specificationId = "test-spec1";
            List<Models.Calcs.Calculation> calculations = new List<Models.Calcs.Calculation>
            {
                new Models.Calcs.Calculation
                {
                    Id = "calcId1",
                    Name = "calc 1"
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository.GetCalculationsBySpecificationId(Arg.Is(specificationId)).Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository.UpdateBuildProject(Arg.Any<BuildProject>()).Returns(HttpStatusCode.OK);

            BuildProjectsService buildProjectsService = CreateBuildProjectsServiceWithRealCompiler(buildProjectsRepository, calculationsRepository: calculationsRepository);

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            // Act
            Func<Task> action = async () => await buildProjectsService.CompileBuildProject(buildProject);

            // Assert
            action.Should().NotThrow();
            buildProject.Build.Success.Should().BeTrue();
        }

        [TestMethod]
        public void CompileBuildProject_WhenBuildingCalculationWithCodeError_ThenFailsToCompiles()
        {
            // Arrange
            string specificationId = "test-spec1";
            List<Models.Calcs.Calculation> calculations = new List<Models.Calcs.Calculation>
            {
                new Models.Calcs.Calculation
                {
                    Id = "calcId1",
                    Name = "calc 1",
                    Current = new CalculationVersion
                    {
                        SourceCode = "return \"abc\""
                    }
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository.GetCalculationsBySpecificationId(Arg.Is(specificationId)).Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository.UpdateBuildProject(Arg.Any<BuildProject>()).Returns(HttpStatusCode.OK);

            BuildProjectsService buildProjectsService = CreateBuildProjectsServiceWithRealCompiler(buildProjectsRepository, calculationsRepository: calculationsRepository);

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            // Act
            Func<Task> action = async () => await buildProjectsService.CompileBuildProject(buildProject);

            // Assert
            action.Should().NotThrow();
            buildProject.Build.Success.Should().BeFalse();
        }

        [TestMethod]
        public void CompileBuildProject_WhenBuildingCalculationUsingExcludeFunction_ThenCompilesSuccessfully()
        {
            // Arrange
            string specificationId = "test-spec1";
            List<Models.Calcs.Calculation> calculations = new List<Models.Calcs.Calculation>
            {
                new Models.Calcs.Calculation
                {
                    Id = "calcId1",
                    Name = "calc 1",
                    Current = new CalculationVersion
                    {
                        SourceCode = "return Exclude()"
                    }
                }
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository.GetCalculationsBySpecificationId(Arg.Is(specificationId)).Returns(calculations);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository.UpdateBuildProject(Arg.Any<BuildProject>()).Returns(HttpStatusCode.OK);

            BuildProjectsService buildProjectsService = CreateBuildProjectsServiceWithRealCompiler(buildProjectsRepository, calculationsRepository: calculationsRepository);

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            // Act
            Func<Task> action = async () => await buildProjectsService.CompileBuildProject(buildProject);

            // Assert
            action.Should().NotThrow();
            buildProject.Build.Success.Should().BeTrue();
        }

        [TestMethod]
        public void UpdateAllocations_GivenBuildProjectNotFound_ThrowsArgumentException()
        {
            //Arrange
            string specificationId = "test-spec1";

            Message message = new Message(Encoding.UTF8.GetBytes(""));

            message.UserProperties.Add("specification-id", specificationId);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
                .Returns((BuildProject)null);

            ILogger logger = CreateLogger();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository, logger: logger);

            //Act
            Func<Task> test = async () => await buildProjectsService.UpdateAllocations(message);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectButNoSummariesInCache_CallsPopulateSummaries()
        {
            //Arrange
            string specificationId = "test-spec1";

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));

            message.UserProperties.Add("specification-id", specificationId);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(false);

            IProviderResultsRepository providerResultsRepository = CreateProviderResultsRepository();

            ILogger logger = CreateLogger();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository, 
                logger: logger, providerResultsRepository: providerResultsRepository, cacheProvider: cacheProvider);

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            await
                providerResultsRepository
                    .Received(1)
                    .PopulateProviderSummariesForSpecification(Arg.Is(specificationId));
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectAndSummariesInCache_DoesntCallPopulateSummaries()
        {
            //Arrange
            string specificationId = "test-spec1";

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));

            message.UserProperties.Add("specification-id", specificationId);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            IProviderResultsRepository providerResultsRepository = CreateProviderResultsRepository();

            ILogger logger = CreateLogger();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository, 
                logger: logger, providerResultsRepository: providerResultsRepository, cacheProvider: cacheProvider);

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            await
                providerResultsRepository
                    .DidNotReceive()
                    .PopulateProviderSummariesForSpecification(Arg.Is(specificationId));
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectAndListLengthOfTenThousandProviders_AddsTenMessagesToQueue()
        {
            //Arrange
            string specificationId = "test-spec1";

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));

            message.UserProperties.Add("specification-id", specificationId);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(10000);

            IMessengerService messengerService = CreateMessengerService();

            IProviderResultsRepository providerResultsRepository = CreateProviderResultsRepository();

            ILogger logger = CreateLogger();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository,
                logger: logger, providerResultsRepository: providerResultsRepository, cacheProvider: cacheProvider,
                messengerService: messengerService);

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            await
                providerResultsRepository
                    .DidNotReceive()
                    .PopulateProviderSummariesForSpecification(Arg.Is(specificationId));

            await
                messengerService
                    .Received(10)
                    .SendToQueue<string>(Arg.Is(ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResults), Arg.Any<string>(), Arg.Any<IDictionary<string, string>>());
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectAndFeatureToggleIsOn_CallsUpdateCalculationLastupdatedDate()
        {
            //Arrange
            string specificationId = "test-spec1";

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));

            message.UserProperties.Add("specification-id", specificationId);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(10000);

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(true);

            ILogger logger = CreateLogger();

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository,
                logger: logger, cacheProvider: cacheProvider, featureToggle: featureToggle, specificationsRepository: specificationRepository);

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            await
                specificationRepository
                    .Received(1)
                    .UpdateCalculationLastUpdatedDate(Arg.Is(specificationId));
        }


        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectAndFeatureToggleIsOff_DoesNotCallUpdateCalculationLastupdatedDate()
        {
            //Arrange
            string specificationId = "test-spec1";

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));

            message.UserProperties.Add("specification-id", specificationId);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectsRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(specificationId))
                .Returns(buildProject);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(10000);

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsAllocationLineMajorMinorVersioningEnabled()
                .Returns(false);

            ILogger logger = CreateLogger();

            ISpecificationRepository specificationRepository = CreateSpecificationRepository();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(buildProjectsRepository,
                logger: logger, cacheProvider: cacheProvider, featureToggle: featureToggle, specificationsRepository: specificationRepository);

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            await
                specificationRepository
                    .DidNotReceive()
                    .UpdateCalculationLastUpdatedDate(Arg.Any<string>());
        }

        [TestMethod]
        public async Task UpdateDeadLetteredJobLog_GivenMessageButNoJobId_LogsAnErrorAndDoesNotUpdadeJobLog()
        {
            //Arrange
            Message message = new Message();

            IJobsRepository jobsRepository = CreateJobsRepository();

            ILogger logger = CreateLogger();

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsJobServiceEnabled()
                .Returns(true);

            BuildProjectsService service = CreateBuildProjectsService(logger: logger, jobsRepository: jobsRepository, featureToggle: featureToggle);

            //Act
            await service.UpdateDeadLetteredJobLog(message);

            //Assert
            logger
                .Received(1)
                .Error(Arg.Is("Missing job id from dead lettered message"));

            await
                jobsRepository
                    .DidNotReceive()
                    .AddJobLog(Arg.Any<string>(), Arg.Any<JobLogUpdateModel>());
        }

        [TestMethod]
        public async Task UpdateDeadLetteredJobLog_GivenMessageButAddingLogCausesException_LogsAnError()
        {
            //Arrange
            const string jobId = "job-id-1";

            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            IJobsRepository jobsRepository = CreateJobsRepository();
            jobsRepository
                    .When(x => x.AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>()))
                    .Do(x => { throw new Exception(); });

            ILogger logger = CreateLogger();

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsJobServiceEnabled()
                .Returns(true);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(jobsRepository: jobsRepository, logger: logger, featureToggle: featureToggle);

            //Act
            await buildProjectsService.UpdateDeadLetteredJobLog(message);

            //Assert
           logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Failed to add a job log for job id '{jobId}'"));
        }

        [TestMethod]
        public async Task UpdateDeadLetteredJobLog_GivenMessageAndLogIsUpdated_LogsInformation()
        {
            //Arrange
            const string jobId = "job-id-1";

            JobLog jobLog = new JobLog
            {
                Id = "job-log-id-1"
            };

            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            IJobsRepository jobsRepository = CreateJobsRepository();
            jobsRepository
                .AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>())
                .Returns(jobLog);

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsJobServiceEnabled()
                .Returns(true);

            ILogger logger = CreateLogger();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(jobsRepository: jobsRepository, logger: logger, featureToggle: featureToggle);

            //Act
            await buildProjectsService.UpdateDeadLetteredJobLog(message);

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"A new job log was added to inform of a dead lettered message with job log id '{jobLog.Id}' on job with id '{jobId}' while attempting to instruct allocations"));
        }

        [TestMethod]
        public async Task UpdateDeadLetteredJobLog_GivenMessageAndFeatureToggleIsOff_DoesNotAddJobLog()
        {
            //Arrange
            const string jobId = "job-id-1";

            JobLog jobLog = new JobLog
            {
                Id = "job-log-id-1"
            };

            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            IJobsRepository jobsRepository = CreateJobsRepository();
            
            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsJobServiceEnabled()
                .Returns(false);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(jobsRepository: jobsRepository, featureToggle: featureToggle);

            //Act
            await buildProjectsService.UpdateDeadLetteredJobLog(message);

            //Assert
            await
                jobsRepository
                    .DidNotReceive()
                    .AddJobLog(Arg.Any<string>(), Arg.Any<JobLogUpdateModel>());
        }

        private BuildProjectsService CreateBuildProjectsServiceWithRealCompiler(IBuildProjectsRepository buildProjectsRepository, ICalculationsRepository calculationsRepository)
        {
            ILogger logger = CreateLogger();
            ISourceFileGeneratorProvider sourceFileGeneratorProvider = CreateSourceFileGeneratorProvider();
            sourceFileGeneratorProvider.CreateSourceFileGenerator(Arg.Is(TargetLanguage.VisualBasic)).Returns(new VisualBasicSourceFileGenerator(logger));
            VisualBasicCompiler vbCompiler = new VisualBasicCompiler(logger);
            CompilerFactory compilerFactory = new CompilerFactory(null, vbCompiler);

            return CreateBuildProjectsService(buildProjectsRepository: buildProjectsRepository, sourceFileGeneratorProvider: sourceFileGeneratorProvider, calculationsRepository: calculationsRepository, logger: logger, compilerFactory: compilerFactory);
        }

        private static BuildProjectsService CreateBuildProjectsService(
            IBuildProjectsRepository buildProjectsRepository = null,
            IMessengerService messengerService = null,
            ILogger logger = null,
            ITelemetry telemetry = null,
            IProviderResultsRepository providerResultsRepository = null,
            ISpecificationRepository specificationsRepository = null,
            ISourceFileGeneratorProvider sourceFileGeneratorProvider = null,
            ICompilerFactory compilerFactory = null,
            ICacheProvider cacheProvider = null,
            ICalculationService calculationService = null,
            ICalculationsRepository calculationsRepository = null,
            IFeatureToggle featureToggle = null,
            IJobsRepository jobsRepository = null)
        {
            return new BuildProjectsService(
                buildProjectsRepository ?? CreateBuildProjectsRepository(),
                messengerService ?? CreateMessengerService(),
                logger ?? CreateLogger(),
                telemetry ?? CreateTelemetry(),
                providerResultsRepository ?? CreateProviderResultsRepository(),
                specificationsRepository ?? CreateSpecificationRepository(),
                sourceFileGeneratorProvider ?? CreateSourceFileGeneratorProvider(),
                compilerFactory ?? CreateCompilerfactory(),
                cacheProvider ?? CreateCacheProvider(),
                calculationService ?? CreateCalculationService(),
                calculationsRepository ?? CreateCalculationsRepository(),
                featureToggle ?? CreateFeatureToggle(),
                jobsRepository ?? CreateJobsRepository(),
                CalcsResilienceTestHelper.GenerateTestPolicies());
        }

        static IFeatureToggle CreateFeatureToggle()
        {
            return Substitute.For<IFeatureToggle>();
        }

        private static Message CreateMessage(string specificationId = SpecificationId)
        {
            dynamic anyObject = new { specificationId };

            string json = JsonConvert.SerializeObject(anyObject);

            return new Message(Encoding.UTF8.GetBytes(json));
        }

        private static ISourceFileGeneratorProvider CreateSourceFileGeneratorProvider()
        {
            return Substitute.For<ISourceFileGeneratorProvider>();
        }

        private static ICompilerFactory CreateCompilerfactory()
        {
            return Substitute.For<ICompilerFactory>();
        }

        private static ICalculationService CreateCalculationService()
        {
            return Substitute.For<ICalculationService>();
        }

        private static ITelemetry CreateTelemetry()
        {
            return Substitute.For<ITelemetry>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        private static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService>();
        }

        private static IBuildProjectsRepository CreateBuildProjectsRepository()
        {
            return Substitute.For<IBuildProjectsRepository>();
        }

        private static Interfaces.IProviderResultsRepository CreateProviderResultsRepository()
        {
            return Substitute.For<Interfaces.IProviderResultsRepository>();
        }

        private static ISpecificationRepository CreateSpecificationRepository()
        {
            return Substitute.For<ISpecificationRepository>();
        }

        private static ICalculationsRepository CreateCalculationsRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        private static IJobsRepository CreateJobsRepository()
        {
            return Substitute.For<IJobsRepository>();
        }
    }
}
