using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using GraphCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Calculation;
using CalculationEntity = CalculateFunding.Models.Graph.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Calculation, CalculateFunding.Common.ApiClient.Graph.Models.Relationship>;
using GraphRelationship = CalculateFunding.Common.ApiClient.Graph.Models.Relationship;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;

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
        public void UpdateBuildProjectRelationships_GivenSpecificationIdKeyyNotFoundOnMessage_ThrowsKeyNotFoundException()
        {
            //Arrange
            DatasetRelationshipSummary payload = new DatasetRelationshipSummary();

            string json = JsonConvert.SerializeObject(payload);

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

            string json = JsonConvert.SerializeObject(payload);

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
        async public Task UpdateBuildProjectRelationships_GivenRelationshipNameAlreadyExists_DoesNotCompileAndUpdate()
        {
            //Arrange
            const string relationshipName = "test--name";

            DatasetRelationshipSummary payload = new DatasetRelationshipSummary
            {
                Name = relationshipName
            };

            string json = JsonConvert.SerializeObject(payload);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
               .UserProperties.Add("specification-id", SpecificationId);

            DatasetSpecificationRelationshipViewModel datasetSpecificationRelationshipViewModel = new DatasetSpecificationRelationshipViewModel
            {
                DatasetId = "ds-1",
                DatasetName = "ds 1",
                Definition = new DatasetDefinitionViewModel
                {
                    Id = "111",
                    Name = "def 1"
                },
                IsProviderData = true,
                Id = "rel-1",
                Name = "test--name"
            };

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = "111"
            };

            IEnumerable<DatasetSpecificationRelationshipViewModel> datasetSpecificationRelationshipViewModels = new[]
            {
                    datasetSpecificationRelationshipViewModel
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetCurrentRelationshipsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(datasetSpecificationRelationshipViewModels);

            datasetRepository
                .GetDatasetDefinitionById(Arg.Is("111"))
                .Returns(datasetDefinition);

            ISourceCodeService sourceCodeService = CreateSourceCodeService();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(sourceCodeService: sourceCodeService, datasetRepository: datasetRepository);

            //Act
            await buildProjectsService.UpdateBuildProjectRelationships(message);

            //Assert
            sourceCodeService
                .DidNotReceive()
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>());
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

            string json = JsonConvert.SerializeObject(payload);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
               .UserProperties.Add("specification-id", SpecificationId);

            DatasetSpecificationRelationshipViewModel datasetSpecificationRelationshipViewModel = new DatasetSpecificationRelationshipViewModel
            {
                DatasetId = "ds-1",
                DatasetName = "ds 1",
                Definition = new DatasetDefinitionViewModel
                {
                    Id = "111",
                    Name = "def 1"
                },
                IsProviderData = true,
                Id = "rel-1",
                Name = "rel 1"
            };

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = "111"
            };

            IEnumerable<DatasetSpecificationRelationshipViewModel> datasetSpecificationRelationshipViewModels = new[]
            {
                    datasetSpecificationRelationshipViewModel
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetCurrentRelationshipsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(datasetSpecificationRelationshipViewModels);

            datasetRepository
                .GetDatasetDefinitionById(Arg.Is("111"))
                .Returns(datasetDefinition);

            ISourceCodeService sourceCodeService = CreateSourceCodeService();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(sourceCodeService: sourceCodeService, datasetRepository: datasetRepository);

            //Act
            await buildProjectsService.UpdateBuildProjectRelationships(message);

            //Assert
            sourceCodeService
                .Received(1)
                .Compile(Arg.Any<BuildProject>(), Arg.Any<IEnumerable<Calculation>>(), Arg.Any<CompilerOptions>());
        }

        [TestMethod]
        public async Task UpdateBuildProjectRelationships_GivenIsDynamicBuildProjectServiceFeatureSwitchedOff_EnsuresUpdatesCosmos()
        {
            //Arrange
            const string relationshipName = "test--name";

            DatasetRelationshipSummary payload = new DatasetRelationshipSummary
            {
                Name = relationshipName
            };

            string json = JsonConvert.SerializeObject(payload);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
               .UserProperties.Add("specification-id", SpecificationId);

            DatasetSpecificationRelationshipViewModel datasetSpecificationRelationshipViewModel = new DatasetSpecificationRelationshipViewModel
            {
                DatasetId = "ds-1",
                DatasetName = "ds 1",
                Definition = new DatasetDefinitionViewModel
                {
                    Id = "111",
                    Name = "def 1"
                },
                IsProviderData = true,
                Id = "rel-1",
                Name = "rel 1"
            };

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = "111"
            };

            IEnumerable<DatasetSpecificationRelationshipViewModel> datasetSpecificationRelationshipViewModels = new[]
            {
                    datasetSpecificationRelationshipViewModel
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetCurrentRelationshipsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(datasetSpecificationRelationshipViewModels);

            datasetRepository
                .GetDatasetDefinitionById(Arg.Is("111"))
                .Returns(datasetDefinition);

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsDynamicBuildProjectEnabled()
                .Returns(false);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectRepository();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(datasetRepository: datasetRepository, buildProjectsRepository: buildProjectsRepository, featureToggle: featureToggle);

            //Act
            await buildProjectsService.UpdateBuildProjectRelationships(message);

            //Assert
            await
                buildProjectsRepository
                    .Received(1)
                    .UpdateBuildProject(Arg.Any<BuildProject>());
        }

        [TestMethod]
        public void UpdateBuildProjectRelationships_GivenRelationshipButFailsToSaveAssembly_ThrowsException()
        {
            //Arrange
            const string relationshipName = "test--name";

            DatasetRelationshipSummary payload = new DatasetRelationshipSummary
            {
                Name = relationshipName
            };

            string json = JsonConvert.SerializeObject(payload);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            message
               .UserProperties.Add("specification-id", SpecificationId);

            DatasetSpecificationRelationshipViewModel datasetSpecificationRelationshipViewModel = new DatasetSpecificationRelationshipViewModel
            {
                DatasetId = "ds-1",
                DatasetName = "ds 1",
                Definition = new DatasetDefinitionViewModel
                {
                    Id = "111",
                    Name = "def 1"
                },
                IsProviderData = true,
                Id = "rel-1",
                Name = "rel-1"
            };

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = "111"
            };

            IEnumerable<DatasetSpecificationRelationshipViewModel> datasetSpecificationRelationshipViewModels = new[]
            {
                    datasetSpecificationRelationshipViewModel
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetCurrentRelationshipsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(datasetSpecificationRelationshipViewModels);

            datasetRepository
                .GetDatasetDefinitionById(Arg.Is("111"))
                .Returns(datasetDefinition);

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService.When(x => x.SaveAssembly(Arg.Any<BuildProject>()))
                                        .Do(x => throw new Exception());

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(sourceCodeService: sourceCodeService, datasetRepository: datasetRepository);

            //Act
            Func<Task> test = () => buildProjectsService.UpdateBuildProjectRelationships(message);

            //Assert
            test
                .Should().ThrowExactly<Exception>();
        }

        [TestMethod]
        public async Task GetBuildProjectBySpecificationId_GivenNoSpecificationId_ReturnsBadRequest()
        {
            //Arrange
            ILogger logger = CreateLogger();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(logger: logger);

            //Act
            IActionResult result = await buildProjectsService.GetBuildProjectBySpecificationId(null);

            //Assert
            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>();

            logger
                .Received(1)
                .Error(Arg.Is("No specification Id was provided to GetBuildProjectBySpecificationId"));
        }

        [TestMethod]
        public async Task GetBuildProjectBySpecificationId_GivenBuildProjectGeneratedButNoDatasetRelationshipsFound_ReturnsOKResult()
        {
            //Arrange
            BuildProjectsService buildProjectsService = CreateBuildProjectsService();

            //Act
            IActionResult result = await buildProjectsService.GetBuildProjectBySpecificationId(SpecificationId);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>();

            OkObjectResult okObjectResult = result as OkObjectResult;

            BuildProject buildProject = okObjectResult.Value as BuildProject;

            buildProject.SpecificationId.Should().Be(SpecificationId);
            buildProject.Id.Should().NotBeEmpty();
            buildProject.DatasetRelationships.Should().BeEmpty();
        }

        [TestMethod]
        public async Task GetBuildProjectBySpecificationId_GivenBuildProjectGeneratedAndDatasetRelationshipsFound_ReturnsOKResult()
        {
            //Arrange
            DatasetSpecificationRelationshipViewModel datasetSpecificationRelationshipViewModel = new DatasetSpecificationRelationshipViewModel
            {
                DatasetId = "ds-1",
                DatasetName = "ds 1",
                Definition = new DatasetDefinitionViewModel
                {
                    Id = "111",
                    Name = "def 1"
                },
                IsProviderData = true,
                Id = "rel-1",
                Name = "rel 1"
            };

            DatasetDefinition datasetDefinition = new DatasetDefinition
            {
                Id = "111"
            };

            IEnumerable<DatasetSpecificationRelationshipViewModel> datasetSpecificationRelationshipViewModels = new[]
            {
                    datasetSpecificationRelationshipViewModel
            };

            IDatasetRepository datasetRepository = CreateDatasetRepository();
            datasetRepository
                .GetCurrentRelationshipsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(datasetSpecificationRelationshipViewModels);

            datasetRepository
                .GetDatasetDefinitionById(Arg.Is("111"))
                .Returns(datasetDefinition);


            BuildProjectsService buildProjectsService = CreateBuildProjectsService(datasetRepository: datasetRepository);

            //Act
            IActionResult result = await buildProjectsService.GetBuildProjectBySpecificationId(SpecificationId);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>();

            OkObjectResult okObjectResult = result as OkObjectResult;

            BuildProject buildProject = okObjectResult.Value as BuildProject;

            buildProject.SpecificationId.Should().Be(SpecificationId);
            buildProject.Id.Should().NotBeEmpty();
            buildProject.DatasetRelationships.Should().HaveCount(1);
            buildProject.DatasetRelationships.First().Id.Should().Be("rel-1");
            buildProject.DatasetRelationships.First().Name.Should().Be("rel 1");
            buildProject.DatasetRelationships.First().DatasetId.Should().Be("ds-1");
            buildProject.DatasetRelationships.First().DatasetDefinitionId.Should().Be("111");
            buildProject.DatasetRelationships.First().DatasetDefinition.Should().Be(datasetDefinition);
        }

        [TestMethod]
        public async Task GetBuildProjectBySpecificationId_GivenIsDynamicBuildProjectFeatureToggleSwitchedOffAndBuildProjectFound_ReturnsOKResult()
        {
            //Arrange
            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsDynamicBuildProjectEnabled()
                .Returns(false);

            BuildProject buildProject = new BuildProject();

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns(buildProject);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(featureToggle: featureToggle, buildProjectsRepository: buildProjectsRepository);

            //Act
            IActionResult result = await buildProjectsService.GetBuildProjectBySpecificationId(SpecificationId);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetBuildProjectBySpecificationId_GivenIsDynamicBuildProjectFeatureToggleSwitchedOffAndBuildProjectNotFound_ReturnsOKResult()
        {
            //Arrange
            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsDynamicBuildProjectEnabled()
                .Returns(false);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectRepository();
            buildProjectsRepository
                .GetBuildProjectBySpecificationId(Arg.Is(SpecificationId))
                .Returns((BuildProject)null);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(featureToggle: featureToggle, buildProjectsRepository: buildProjectsRepository);

            //Act
            IActionResult result = await buildProjectsService.GetBuildProjectBySpecificationId(SpecificationId);

            //Assert
            result
                .Should()
                .BeAssignableTo<OkObjectResult>();
        }

        [TestMethod]
        public async Task GetAssemblyBySpecificationId_GivenNoSpecificationId_ReturnsBadRequest()
        {
            //Arrange
            const string specificationId = "";

            ILogger logger = CreateLogger();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(logger: logger);

            //Act
            IActionResult result = await buildProjectsService.GetAssemblyBySpecificationId(specificationId);

            //Assert
            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Null or empty specificationId provided");

            logger
                .Received(1)
                .Error(Arg.Is("No specificationId was provided to GetAssemblyBySpecificationId"));
        }

        [TestMethod]
        public async Task GetAssemblyBySpecificationId_GivenBuildProjectFoundButReturnsEmptyAssembly_ReturnsInternalServerErrorResult()
        {
            //Arrange
            const string specificationId = "spec-id-1";

            ILogger logger = CreateLogger();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .GetAssembly(Arg.Any<BuildProject>())
                .Returns(new byte[0]);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(logger: logger, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await buildProjectsService.GetAssemblyBySpecificationId(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Failed to get assembly for specification id '{specificationId}'");

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to get assembly for specification id '{specificationId}'"));
        }

        [TestMethod]
        public async Task GetAssemblyBySpecificationId_GivenBuildProjectFoundAndGetsAssembly_ReturnsOKObjectResult()
        {
            //Arrange
            const string specificationId = "spec-id-1";

            ILogger logger = CreateLogger();

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .GetAssembly(Arg.Any<BuildProject>(), Arg.Is(false))
                .Returns(new byte[100]);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(logger: logger, sourceCodeService: sourceCodeService);

            //Act
            IActionResult result = await buildProjectsService.GetAssemblyBySpecificationId(specificationId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>();
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectButNoSummariesInCache_CallsRegeneratePopulateScopedProviders()
        {
            //Arrange
            string specificationId = "test-spec1";
            string parentJobId = "job-id-1";
            string jobId = "job-id-2";

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", jobId);
            message.UserProperties.Add("specification-id", specificationId);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(false);

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .RegenerateProviderSummariesForSpecification(Arg.Is(specificationId), Arg.Is(true))
                .Returns(new ApiResponse<bool>(HttpStatusCode.OK, true));

            ILogger logger = CreateLogger();

            JobViewModel parentJob = new JobViewModel
            {
                Id = parentJobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1",
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob
            };

            ApiResponse<JobViewModel> parentJobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            JobViewModel childJob = new JobViewModel
            {
                Id = jobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1",
                JobDefinitionId = JobConstants.DefinitionNames.GenerateCalculationAggregationsJob
            };

            ApiResponse<JobViewModel> jobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            IJobManagement jobManagement = CreateJobManagement();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJobViewModelResponse);
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModelResponse);
            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(new List<Job> { new Job { SpecificationId = specificationId } });
            jobManagement.WaitForJobsToComplete(Arg.Is<IEnumerable<string>>(_ => _.Single() == JobConstants.DefinitionNames.PopulateScopedProvidersJob), specificationId)
                .Returns(true);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(jobsApiClient: jobsApiClient,
                logger: logger, providersApiClient: providersApiClient, cacheProvider: cacheProvider, jobManagement: jobManagement);

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            await
                providersApiClient
                    .Received(1)
                    .RegenerateProviderSummariesForSpecification(Arg.Is(specificationId), Arg.Is(true));
        }
        
        [TestMethod]
        public void UpdateAllocations_GivenBuildProjectButNoSummariesInCacheRegeneratePopulateScopedProvidersFails_ExceptionThrown()
        {
            //Arrange
            string specificationId = "test-spec1";
            string parentJobId = "job-id-1";
            string jobId = "job-id-2";

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", jobId);
            message.UserProperties.Add("specification-id", specificationId);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(false);

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .RegenerateProviderSummariesForSpecification(Arg.Is(specificationId), Arg.Is(true))
                .Returns(new ApiResponse<bool>(HttpStatusCode.BadRequest));

            ILogger logger = CreateLogger();

            JobViewModel parentJob = new JobViewModel
            {
                Id = parentJobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1",
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob
            };

            ApiResponse<JobViewModel> parentJobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            JobViewModel childJob = new JobViewModel
            {
                Id = jobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1",
                JobDefinitionId = JobConstants.DefinitionNames.GenerateCalculationAggregationsJob
            };

            ApiResponse<JobViewModel> jobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            IJobManagement jobManagement = CreateJobManagement();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJobViewModelResponse);
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModelResponse);
            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(new List<Job> { new Job { SpecificationId = specificationId } });
            jobManagement.WaitForJobsToComplete(Arg.Is<IEnumerable<string>>(_ => _.Single() == JobConstants.DefinitionNames.PopulateScopedProvidersJob), specificationId)
                .Returns(true);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(jobsApiClient: jobsApiClient,
                logger: logger, providersApiClient: providersApiClient, cacheProvider: cacheProvider, jobManagement: jobManagement);

            //Act
            Func<Task> invocation = async() => await buildProjectsService.UpdateAllocations(message);

            //Assert
            invocation
                .Should()
                .Throw<RetriableException>()
                .WithMessage($"Unable to re-generate scoped providers while building projects '{specificationId}' with status code: {HttpStatusCode.BadRequest}");
        }

        [TestMethod]
        public void UpdateAllocations_GivenBuildProjectButNoSummariesInCacheRegeneratePopulateScopedProvidersJobFails_ExceptionThrown()
        {
            //Arrange
            string specificationId = "test-spec1";
            string parentJobId = "job-id-1";
            string jobId = "job-id-2";

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", jobId);
            message.UserProperties.Add("specification-id", specificationId);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(false);

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .RegenerateProviderSummariesForSpecification(Arg.Is(specificationId), Arg.Is(true))
                .Returns(new ApiResponse<bool>(HttpStatusCode.OK, true));

            ILogger logger = CreateLogger();

            JobViewModel parentJob = new JobViewModel
            {
                Id = parentJobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1",
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob
            };

            ApiResponse<JobViewModel> parentJobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            JobViewModel childJob = new JobViewModel
            {
                Id = jobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1",
                JobDefinitionId = JobConstants.DefinitionNames.GenerateCalculationAggregationsJob
            };

            ApiResponse<JobViewModel> jobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            IJobManagement jobManagement = CreateJobManagement();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJobViewModelResponse);
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModelResponse);
            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(new List<Job> { new Job { SpecificationId = specificationId } });
            jobManagement.WaitForJobsToComplete(Arg.Is<IEnumerable<string>>(_ => _.Single() == JobConstants.DefinitionNames.PopulateScopedProvidersJob), specificationId)
                .Returns(false);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(jobsApiClient: jobsApiClient,
                logger: logger, providersApiClient: providersApiClient, cacheProvider: cacheProvider, jobManagement: jobManagement);

            //Act
            Func<Task> invocation = async () => await buildProjectsService.UpdateAllocations(message);

            //Assert
            invocation
                .Should()
                .Throw<RetriableException>()
                .WithMessage($"Unable to re-generate scoped providers while building projects '{specificationId}' job didn't complete successfully in time");
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenMessageDoesNotHaveAJobId_DoesntAddAJobLog()
        {
            //Arrange
            Message message = new Message(Encoding.UTF8.GetBytes(""));

            ILogger logger = CreateLogger();

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(logger: logger, jobsApiClient: jobsApiClient);

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            await
                jobsApiClient
                    .DidNotReceive()
                    .AddJobLog(Arg.Any<string>(), Arg.Any<JobLogUpdateModel>());

            logger
                .Received(1)
                .Error(Arg.Is("Missing parent job id to instruct generating allocations"));
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectAndSummariesInCache_DoesntCallPopulateSummaries()
        {
            //Arrange
            IEnumerable<ProviderSummary> providerSummaries = new[]
            {
                new ProviderSummary{ Id = "1" },
                new ProviderSummary{ Id = "2" },
                new ProviderSummary{ Id = "3" },
                new ProviderSummary{ Id = "4" },
                new ProviderSummary{ Id = "5" },
                new ProviderSummary{ Id = "6" },
                new ProviderSummary{ Id = "7" },
                new ProviderSummary{ Id = "8" },
                new ProviderSummary{ Id = "9" },
                new ProviderSummary{ Id = "10" }
            };

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

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(cacheKey)
                .Returns(10);

            IEnumerable<string> providerIds = providerSummaries.Select(m => m.Id);

            cacheProvider
                .ListRangeAsync<ProviderSummary>(Arg.Is(cacheKey), Arg.Is(0), Arg.Is(10))
                .Returns(providerSummaries);

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetScopedProviderIds(Arg.Is(specificationId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, providerIds));

            ILogger logger = CreateLogger();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(
                logger: logger, cacheProvider: cacheProvider, providersApiClient: providersApiClient);

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            await
                providersApiClient
                    .DidNotReceive()
                    .RegenerateProviderSummariesForSpecification(Arg.Is(specificationId));
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectAndListLengthOfTenThousandProviders_CreatesTenJobs()
        {
            //Arrange
            EngineSettings engineSettings = CreateEngineSettings();
            engineSettings.MaxPartitionSize = 1;

            IEnumerable<ProviderSummary> providerSummaries = new[]
            {
                new ProviderSummary{ Id = "1" },
                new ProviderSummary{ Id = "2" },
                new ProviderSummary{ Id = "3" },
                new ProviderSummary{ Id = "4" },
                new ProviderSummary{ Id = "5" },
                new ProviderSummary{ Id = "6" },
                new ProviderSummary{ Id = "7" },
                new ProviderSummary{ Id = "8" },
                new ProviderSummary{ Id = "9" },
                new ProviderSummary{ Id = "10" }
            };

            string parentJobId = "job-id-1";

            string specificationId = "test-spec1";

            JobViewModel parentJob = new JobViewModel
            {
                Id = parentJobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1"
            };

            ApiResponse<JobViewModel> jobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", "job-id-1");
            message.UserProperties.Add("specification-id", specificationId);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(cacheKey)
                .Returns(10);

            IEnumerable<string> providerIds = providerSummaries.Select(m => m.Id);

            cacheProvider
                .ListRangeAsync<ProviderSummary>(Arg.Is(cacheKey), Arg.Is(0), Arg.Is(10))
                .Returns(providerSummaries);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(parentJobId))
                .Returns(jobViewModelResponse);

            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(CreateJobs());

            ILogger logger = CreateLogger();

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetScopedProviderIds(Arg.Is(specificationId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, providerIds));

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(
                logger: logger, cacheProvider: cacheProvider,
                jobsApiClient: jobsApiClient, engineSettings: engineSettings, providersApiClient: providersApiClient);

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            await
                providersApiClient
                    .DidNotReceive()
                    .RegenerateProviderSummariesForSpecification(Arg.Is(specificationId));

            await
                jobsApiClient
                    .Received(1)
                    .CreateJobs(Arg.Is<IEnumerable<JobCreateModel>>(
                            m => m.Count() == 10 &&
                            m.Count(p => p.SpecificationId == specificationId) == 10 &&
                            m.Count(p => p.ParentJobId == parentJobId) == 10 &&
                            m.Count(p => p.InvokerUserDisplayName == parentJob.InvokerUserDisplayName) == 10 &&
                            m.Count(p => p.InvokerUserId == parentJob.InvokerUserId) == 10 &&
                            m.Count(p => p.CorrelationId == parentJob.CorrelationId) == 10 &&
                            m.Count(p => p.Trigger.EntityId == parentJob.Id) == 10 &&
                            m.Count(p => p.Trigger.EntityType == nameof(Job)) == 10 &&
                            m.Count(p => p.Trigger.Message == $"Triggered by parent job with id: '{parentJob.Id}") == 10
                        ));

            logger
                .Received(1)
                .Information($"10 child jobs were created for parent id: '{parentJobId}'");

            await
                jobsApiClient
                    .Received(1)
                    .AddJobLog(Arg.Is(parentJobId), Arg.Any<JobLogUpdateModel>());
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectAndProviderListIsNotAMultipleOfTheBatchSize_CreatesJobsWithCorrectBatches()
        {
            //Arrange
            EngineSettings engineSettings = CreateEngineSettings();
            engineSettings.MaxPartitionSize = 1;

            IEnumerable<ProviderSummary> providerSummaries = new[]
            {
                new ProviderSummary{ Id = "1" },
                new ProviderSummary{ Id = "2" },
                new ProviderSummary{ Id = "3" },
                new ProviderSummary{ Id = "4" },
                new ProviderSummary{ Id = "5" },
                new ProviderSummary{ Id = "6" },
                new ProviderSummary{ Id = "7" },
                new ProviderSummary{ Id = "8" },
                new ProviderSummary{ Id = "9" },
                new ProviderSummary{ Id = "10" }
            };

            string parentJobId = "job-id-1";

            string specificationId = "test-spec1";

            JobViewModel parentJob = new JobViewModel
            {
                Id = parentJobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1"
            };

            ApiResponse<JobViewModel> jobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", "job-id-1");
            message.UserProperties.Add("specification-id", specificationId);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(cacheKey)
                .Returns(10);

            IEnumerable<string> providerIds = providerSummaries.Select(m => m.Id);

            cacheProvider
                .ListRangeAsync<ProviderSummary>(Arg.Is(cacheKey), Arg.Is(0), Arg.Is(10))
                .Returns(providerSummaries);

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetScopedProviderIds(Arg.Is(specificationId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, providerIds));

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            jobsApiClient
                .GetJobById(Arg.Is(parentJobId))
                .Returns(jobViewModelResponse);

            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(CreateJobs());

            ILogger logger = CreateLogger();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(
                logger: logger, providersApiClient: providersApiClient, cacheProvider: cacheProvider,
                jobsApiClient: jobsApiClient, engineSettings: engineSettings);

            IEnumerable<JobCreateModel> jobModelsToTest = null;

            jobsApiClient
                .When(x => x.CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>()))
                .Do(y => jobModelsToTest = y.Arg<IEnumerable<JobCreateModel>>());

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            jobModelsToTest.Should().HaveCount(10);
            jobModelsToTest.ElementAt(0).Properties["provider-summaries-partition-index"].Should().Be("0");
            jobModelsToTest.ElementAt(1).Properties["provider-summaries-partition-index"].Should().Be("1");
            jobModelsToTest.ElementAt(2).Properties["provider-summaries-partition-index"].Should().Be("2");
            jobModelsToTest.ElementAt(3).Properties["provider-summaries-partition-index"].Should().Be("3");
            jobModelsToTest.ElementAt(4).Properties["provider-summaries-partition-index"].Should().Be("4");
            jobModelsToTest.ElementAt(5).Properties["provider-summaries-partition-index"].Should().Be("5");
            jobModelsToTest.ElementAt(6).Properties["provider-summaries-partition-index"].Should().Be("6");
            jobModelsToTest.ElementAt(7).Properties["provider-summaries-partition-index"].Should().Be("7");
            jobModelsToTest.ElementAt(8).Properties["provider-summaries-partition-index"].Should().Be("8");
            jobModelsToTest.ElementAt(9).Properties["provider-summaries-partition-index"].Should().Be("9");
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectAndProviderListIsNotAMultipleOfTheBatchSizeAndMaxPartitionSizeIs1_CreatesJobsWithCorrectBatches()
        {
            //Arrange
            IEnumerable<ProviderSummary> providerSummaries = new[]
            {
                new ProviderSummary{ Id = "1" },
                new ProviderSummary{ Id = "2" },
                new ProviderSummary{ Id = "3" },
                new ProviderSummary{ Id = "4" },
                new ProviderSummary{ Id = "5" },
                new ProviderSummary{ Id = "6" },
                new ProviderSummary{ Id = "7" },
                new ProviderSummary{ Id = "8" },
                new ProviderSummary{ Id = "9" },
                new ProviderSummary{ Id = "10" }
            };

            string parentJobId = "job-id-1";

            string specificationId = "test-spec1";

            JobViewModel parentJob = new JobViewModel
            {
                Id = parentJobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1"
            };

            ApiResponse<JobViewModel> jobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", "job-id-1");
            message.UserProperties.Add("specification-id", specificationId);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(cacheKey)
                .Returns(10);

            IEnumerable<string> providerIds = providerSummaries.Select(m => m.Id);

            cacheProvider
                .ListRangeAsync<ProviderSummary>(Arg.Is(cacheKey), Arg.Is(0), Arg.Is(10))
                .Returns(providerSummaries);

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetScopedProviderIds(Arg.Is(specificationId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, providerIds));

            IJobsApiClient jobsApiClient = CreateJobsApiClient();

            jobsApiClient
                .GetJobById(Arg.Is(parentJobId))
                .Returns(jobViewModelResponse);

            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(CreateJobs());

            ILogger logger = CreateLogger();

            EngineSettings engineSettings = CreateEngineSettings(1);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(
                logger: logger, providersApiClient: providersApiClient, cacheProvider: cacheProvider,
                jobsApiClient: jobsApiClient, engineSettings: engineSettings);

            IEnumerable<JobCreateModel> jobModelsToTest = null;

            jobsApiClient
                .When(x => x.CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>()))
                .Do(y => jobModelsToTest = y.Arg<IEnumerable<JobCreateModel>>());

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            jobModelsToTest.Should().HaveCount(10);
            jobModelsToTest.ElementAt(0).Properties["provider-summaries-partition-index"].Should().Be("0");
            jobModelsToTest.ElementAt(1).Properties["provider-summaries-partition-index"].Should().Be("1");
            jobModelsToTest.ElementAt(2).Properties["provider-summaries-partition-index"].Should().Be("2");
            jobModelsToTest.ElementAt(3).Properties["provider-summaries-partition-index"].Should().Be("3");
            jobModelsToTest.ElementAt(4).Properties["provider-summaries-partition-index"].Should().Be("4");
            jobModelsToTest.ElementAt(5).Properties["provider-summaries-partition-index"].Should().Be("5");
            jobModelsToTest.ElementAt(6).Properties["provider-summaries-partition-index"].Should().Be("6");
            jobModelsToTest.ElementAt(7).Properties["provider-summaries-partition-index"].Should().Be("7");
            jobModelsToTest.ElementAt(8).Properties["provider-summaries-partition-index"].Should().Be("8");
            jobModelsToTest.ElementAt(9).Properties["provider-summaries-partition-index"].Should().Be("9");
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectAndListLengthOfTenProvidersButOnlyCreatedFiveJobs_ThrowsExceptionLogsAnError()
        {
            //Arrange
            EngineSettings engineSettings = CreateEngineSettings();
            engineSettings.MaxPartitionSize = 1;

            IEnumerable<ProviderSummary> providerSummaries = new[]
            {
                new ProviderSummary{ Id = "1" },
                new ProviderSummary{ Id = "2" },
                new ProviderSummary{ Id = "3" },
                new ProviderSummary{ Id = "4" },
                new ProviderSummary{ Id = "5" },
                new ProviderSummary{ Id = "6" },
                new ProviderSummary{ Id = "7" },
                new ProviderSummary{ Id = "8" },
                new ProviderSummary{ Id = "9" },
                new ProviderSummary{ Id = "10" }
            };

            string parentJobId = "job-id-1";

            string specificationId = "test-spec1";

            JobViewModel parentJob = new JobViewModel
            {
                Id = parentJobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId
            };

            ApiResponse<JobViewModel> jobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", "job-id-1");
            message.UserProperties.Add("specification-id", specificationId);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(cacheKey)
                .Returns(10);

            IEnumerable<string> providerIds = providerSummaries.Select(m => m.Id);

            cacheProvider
                .ListRangeAsync<ProviderSummary>(Arg.Is(cacheKey), Arg.Is(0), Arg.Is(10))
                .Returns(providerSummaries);

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetScopedProviderIds(Arg.Is(specificationId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, providerIds));

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(parentJobId))
                .Returns(jobViewModelResponse);

            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(CreateJobs(5));

            ILogger logger = CreateLogger();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(
                logger: logger, providersApiClient: providersApiClient, cacheProvider: cacheProvider,
                jobsApiClient: jobsApiClient, engineSettings: engineSettings);

            //Act
            Func<Task> test = async () => await buildProjectsService.UpdateAllocations(message);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Only 5 child jobs from 10 were created with parent id: 'job-id-1'");


            await
                jobsApiClient
                    .Received(1)
                    .CreateJobs(Arg.Is<IEnumerable<JobCreateModel>>(
                            m => m.Count() == 10 &&
                            m.Count(p => p.SpecificationId == specificationId) == 10 &&
                            m.Count(p => p.ParentJobId == parentJobId) == 10 &&
                            m.Count(p => p.InvokerUserDisplayName == parentJob.InvokerUserDisplayName) == 10 &&
                            m.Count(p => p.InvokerUserId == parentJob.InvokerUserId) == 10 &&
                            m.Count(p => p.Trigger.EntityId == parentJob.Id) == 10 &&
                            m.Count(p => p.Trigger.EntityType == nameof(Job)) == 10 &&
                            m.Count(p => p.Trigger.Message == $"Triggered by parent job with id: '{parentJob.Id}") == 10
                        ));

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), $"Failed to create child jobs for parent job: '{parentJob.Id}'");

            await
                jobsApiClient
                    .Received(1)
                    .AddJobLog(Arg.Is(parentJobId), Arg.Any<JobLogUpdateModel>());
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectAndListLengthOfTenThousandProvidersButParentJobNotFound_ThrowsExceptionLogsAnError()
        {
            //Arrange
            string parentJobId = "job-id-1";

            string specificationId = "test-spec1";

            JobViewModel parentJob = new JobViewModel
            {
                Id = parentJobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId
            };

            ApiResponse<JobViewModel> jobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", "job-id-1");
            message.UserProperties.Add("specification-id", specificationId);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(10000);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(parentJobId))
                .Returns((ApiResponse<JobViewModel>)null);

            ILogger logger = CreateLogger();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(
                logger: logger, cacheProvider: cacheProvider,
                jobsApiClient: jobsApiClient);

            //Act
            Func<Task> test = async () => await buildProjectsService.UpdateAllocations(message);

            //Assert
            test
                .Should()
                .ThrowExactly<Exception>()
                .Which
                .Message
                .Should()
                .Be($"Could not find the parent job with job id: '{parentJobId}'");

            logger
                .Received(1)
                .Error($"Could not find the parent job with job id: '{parentJobId}'");

            await
                jobsApiClient
                    .DidNotReceive()
                    .AddJobLog(Arg.Is(parentJobId), Arg.Any<JobLogUpdateModel>());
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectAndJobFoundButAlreadyInCompletedState_LogsAndReturns()
        {
            //Arrange
            string jobId = "job-id-1";

            string specificationId = "test-spec1";

            JobViewModel job = new JobViewModel
            {
                Id = jobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CompletionStatus = CompletionStatus.Superseded
            };

            ApiResponse<JobViewModel> jobResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, job);

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", "job-id-1");
            message.UserProperties.Add("specification-id", specificationId);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobResponse);

            ILogger logger = CreateLogger();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(
                logger: logger, cacheProvider: cacheProvider,
                jobsApiClient: jobsApiClient);

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            logger
                .Received(1)
                .Information($"Received job with id: '{jobId}' is already in a completed state with status {job.CompletionStatus.ToString()}");

            await
                jobsApiClient
                    .DidNotReceive()
                    .AddJobLog(Arg.Any<string>(), Arg.Any<JobLogUpdateModel>());
        }

        [TestMethod]
        public void UpdateAllocations_GivenSpecificationHasCirculardependencies_ExceptionThrown()
        {
            string parentJobId = "job-id-1";

            string specificationId = "test-spec1";

            JobViewModel parentJob = new JobViewModel
            {
                Id = parentJobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1",
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob
            };

            ApiResponse<JobViewModel> jobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", "job-id-1");
            message.UserProperties.Add("specification-id", specificationId);

            GraphCalculation calculation1 = new GraphCalculation
            {
                CalculationId = "1",
                CalculationName = "Calc 1"
            };

            GraphCalculation calculation2 = new GraphCalculation
            {
                CalculationId = "2",
                CalculationName = "Calc 2"
            };

            GraphRelationship calculation1Relationship = Substitute.For<GraphRelationship>();
            calculation1Relationship.One = calculation1;
            calculation1Relationship.Two = calculation2;

            GraphRelationship calculation2Relationship = Substitute.For<GraphRelationship>();
            calculation2Relationship.One = calculation2;
            calculation2Relationship.Two = calculation1;

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(parentJobId))
                .Returns(jobViewModelResponse);

            ILogger logger = CreateLogger();

            IGraphRepository graphRepository = CreateGraphRepository();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(
                logger: logger, jobsApiClient: jobsApiClient, graphRepository: graphRepository);

            graphRepository.GetCircularDependencies(specificationId)
                .Returns(new[] { new CalculationEntity {
                    Node = calculation1,
                    Relationships = new[] { calculation2Relationship, calculation1Relationship }
                }});

            //Act
            Func<Task> invocation = async() => await buildProjectsService.UpdateAllocations(message);

            //Assert
            invocation
                .Should()
                .Throw<NonRetriableException>()
                .WithMessage($"circular dependencies exist for specification: '{specificationId}'");

            logger.Received(1).Information("Calc 1\r\n|--->Calc 2\r\n   |--->Calc 1");
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectAndListLengthOfTenProvidersAndIsAggregationJobAndOneAggregatedCalcsFound_CreatesTenJobs()
        {
            //Arrange
            EngineSettings engineSettings = CreateEngineSettings();
            engineSettings.MaxPartitionSize = 1;

            IEnumerable<ProviderSummary> providerSummaries = new[]
            {
                new ProviderSummary{ Id = "1" },
                new ProviderSummary{ Id = "2" },
                new ProviderSummary{ Id = "3" },
                new ProviderSummary{ Id = "4" },
                new ProviderSummary{ Id = "5" },
                new ProviderSummary{ Id = "6" },
                new ProviderSummary{ Id = "7" },
                new ProviderSummary{ Id = "8" },
                new ProviderSummary{ Id = "9" },
                new ProviderSummary{ Id = "10" }
            };

            string parentJobId = "job-id-1";

            string specificationId = "test-spec1";

            JobViewModel parentJob = new JobViewModel
            {
                Id = parentJobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1",
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob
            };

            ApiResponse<JobViewModel> jobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", "job-id-1");
            message.UserProperties.Add("specification-id", specificationId);
            message.UserProperties.Add("provider-cache-key", $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}");
            message.UserProperties.Add("ignore-save-provider-results", "true");

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(cacheKey)
                .Returns(10);

            IEnumerable<string> providerIds = providerSummaries.Select(m => m.Id);

            cacheProvider
                .ListRangeAsync<ProviderSummary>(Arg.Is(cacheKey), Arg.Is(0), Arg.Is(10))
                .Returns(providerSummaries);

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetScopedProviderIds(Arg.Is(specificationId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, providerIds));

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(parentJobId))
                .Returns(jobViewModelResponse);

            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(CreateJobs());

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(new[]
                {
                    new Models.Calcs.Calculation
                    {
                        Current = new CalculationVersion
                        {
                             Name = "Calc 1",
                            SourceCode = "return Sum(Calc2)"
                        }
                    },
                    new Models.Calcs.Calculation
                    {
                        Current = new CalculationVersion
                        {
                            Name = "Calc 2",
                            SourceCode = "return 1000"
                        }
                    }
                });

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(
                logger: logger, cacheProvider: cacheProvider,
                jobsApiClient: jobsApiClient, calculationsRepository: calculationsRepository, engineSettings: engineSettings, providersApiClient: providersApiClient);

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            await
                providersApiClient
                    .DidNotReceive()
                    .RegenerateProviderSummariesForSpecification(Arg.Is(specificationId));

            await
                jobsApiClient
                    .Received(1)
                    .CreateJobs(Arg.Is<IEnumerable<JobCreateModel>>(
                            m => m.Count() == 10 &&
                            m.Count(p => p.SpecificationId == specificationId) == 10 &&
                            m.Count(p => p.ParentJobId == parentJobId) == 10 &&
                            m.Count(p => p.InvokerUserDisplayName == parentJob.InvokerUserDisplayName) == 10 &&
                            m.Count(p => p.InvokerUserId == parentJob.InvokerUserId) == 10 &&
                            m.Count(p => p.CorrelationId == parentJob.CorrelationId) == 10 &&
                            m.Count(p => p.Trigger.EntityId == parentJob.Id) == 10 &&
                            m.Count(p => p.Trigger.EntityType == nameof(Job)) == 10 &&
                            m.Count(p => p.Trigger.Message == $"Triggered by parent job with id: '{parentJob.Id}") == 10 &&
                            m.Count(p => p.Properties["ignore-save-provider-results"] == "true") == 10 &&
                            m.Count(p => p.Properties["calculations-to-aggregate"] == "Calc2") == 10 &&
                            m.ElementAt(0).Properties["batch-number"] == "1" &&
                            m.ElementAt(1).Properties["batch-number"] == "2" &&
                            m.ElementAt(2).Properties["batch-number"] == "3" &&
                            m.ElementAt(3).Properties["batch-number"] == "4" &&
                            m.ElementAt(4).Properties["batch-number"] == "5" &&
                            m.ElementAt(5).Properties["batch-number"] == "6" &&
                            m.ElementAt(6).Properties["batch-number"] == "7" &&
                            m.ElementAt(7).Properties["batch-number"] == "8" &&
                            m.ElementAt(8).Properties["batch-number"] == "9" &&
                            m.ElementAt(9).Properties["batch-number"] == "10"
                        ));

            logger
                .Received(1)
                .Information($"10 child jobs were created for parent id: '{parentJobId}'");

            await
                jobsApiClient
                    .Received(1)
                    .AddJobLog(Arg.Is(parentJobId), Arg.Any<JobLogUpdateModel>());

            await
                cacheProvider
                    .Received(1)
                    .RemoveByPatternAsync(Arg.Is($"{CacheKeys.CalculationAggregations}{specificationId}"));
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectAndListLengthOfTenProvidersAndIsAggregationJobAndTwoAggregatedCalcsFound_CreatesTenJobs()
        {
            //Arrange
            EngineSettings engineSettings = CreateEngineSettings();
            engineSettings.MaxPartitionSize = 1;

            IEnumerable<ProviderSummary> providerSummaries = new[]
            {
                new ProviderSummary{ Id = "1" },
                new ProviderSummary{ Id = "2" },
                new ProviderSummary{ Id = "3" },
                new ProviderSummary{ Id = "4" },
                new ProviderSummary{ Id = "5" },
                new ProviderSummary{ Id = "6" },
                new ProviderSummary{ Id = "7" },
                new ProviderSummary{ Id = "8" },
                new ProviderSummary{ Id = "9" },
                new ProviderSummary{ Id = "10" }
            };

            string parentJobId = "job-id-1";

            string specificationId = "test-spec1";

            JobViewModel parentJob = new JobViewModel
            {
                Id = parentJobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1",
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob
            };

            ApiResponse<JobViewModel> jobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", "job-id-1");
            message.UserProperties.Add("specification-id", specificationId);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(cacheKey)
                .Returns(10);

            IEnumerable<string> providerIds = providerSummaries.Select(m => m.Id);

            cacheProvider
                .ListRangeAsync<ProviderSummary>(Arg.Is(cacheKey), Arg.Is(0), Arg.Is(10))
                .Returns(providerSummaries);

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetScopedProviderIds(Arg.Is(specificationId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, providerIds));

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(parentJobId))
                .Returns(jobViewModelResponse);

            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(CreateJobs());

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(new[]
                {
                    new Calculation
                    {
                        Current = new CalculationVersion
                        {
                            Name = "Calc 1",
                            SourceCode = "return Sum(Calc2)"
                        }
                    },
                    new Calculation
                    {
                        Current = new CalculationVersion
                        {
                            Name = "Calc 2",
                            SourceCode = "return 1000"
                        }
                    },
                    new Calculation
                    {
                        Current = new CalculationVersion
                        {
                            Name = "Calc 3",
                            SourceCode = "return Sum(Calc4)"
                        }
                    },
                    new Calculation
                    {
                        Current = new CalculationVersion
                        {
                            Name = "Calc 4",
                            SourceCode = "return 1000"
                        }
                    }
                });

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(
                logger: logger, cacheProvider: cacheProvider,
                jobsApiClient: jobsApiClient, calculationsRepository: calculationsRepository, engineSettings: engineSettings, providersApiClient: providersApiClient);

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            await
                providersApiClient
                    .DidNotReceive()
                    .RegenerateProviderSummariesForSpecification(Arg.Is(specificationId));

            await
                jobsApiClient
                    .Received(1)
                    .CreateJobs(Arg.Is<IEnumerable<JobCreateModel>>(
                            m => m.Count() == 10 &&
                            m.Count(p => p.SpecificationId == specificationId) == 10 &&
                            m.Count(p => p.ParentJobId == parentJobId) == 10 &&
                            m.Count(p => p.InvokerUserDisplayName == parentJob.InvokerUserDisplayName) == 10 &&
                            m.Count(p => p.InvokerUserId == parentJob.InvokerUserId) == 10 &&
                            m.Count(p => p.CorrelationId == parentJob.CorrelationId) == 10 &&
                            m.Count(p => p.Trigger.EntityId == parentJob.Id) == 10 &&
                            m.Count(p => p.Trigger.EntityType == nameof(Job)) == 10 &&
                            m.Count(p => p.Trigger.Message == $"Triggered by parent job with id: '{parentJob.Id}") == 10 &&
                            m.Count(p => p.Properties["calculations-to-aggregate"] == "Calc2,Calc4") == 10 &&
                            m.ElementAt(0).Properties["batch-number"] == "1" &&
                            m.ElementAt(1).Properties["batch-number"] == "2" &&
                            m.ElementAt(2).Properties["batch-number"] == "3" &&
                            m.ElementAt(3).Properties["batch-number"] == "4" &&
                            m.ElementAt(4).Properties["batch-number"] == "5" &&
                            m.ElementAt(5).Properties["batch-number"] == "6" &&
                            m.ElementAt(6).Properties["batch-number"] == "7" &&
                            m.ElementAt(7).Properties["batch-number"] == "8" &&
                            m.ElementAt(8).Properties["batch-number"] == "9" &&
                            m.ElementAt(9).Properties["batch-number"] == "10"
                        ));

            logger
                .Received(1)
                .Information($"10 child jobs were created for parent id: '{parentJobId}'");

            await
                jobsApiClient
                    .Received(1)
                    .AddJobLog(Arg.Is(parentJobId), Arg.Any<JobLogUpdateModel>());

            await
               cacheProvider
                   .Received(1)
                   .RemoveByPatternAsync(Arg.Is($"{CacheKeys.CalculationAggregations}{specificationId}"));
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectButNoScopedProviders_DoesNotCreateChildJobs()
        {
            //Arrange
            string parentJobId = "job-id-1";

            string specificationId = "test-spec1";

            JobViewModel parentJob = new JobViewModel
            {
                Id = parentJobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1",
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob
            };

            ApiResponse<JobViewModel> jobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", "job-id-1");
            message.UserProperties.Add("specification-id", specificationId);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(0);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(parentJobId))
                .Returns(jobViewModelResponse);

            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(CreateJobs());

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetScopedProviderIds(Arg.Is(specificationId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, Enumerable.Empty<string>()));

            ILogger logger = CreateLogger();

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(specificationId))
                .Returns(new[]
                {
                    new Calculation
                    {
                        Current = new CalculationVersion
                        {
                            Name = "Calc 1",
                            SourceCode = "return Sum(Calc2)"
                        }
                    },
                    new Calculation
                    {
                        Current = new CalculationVersion
                        {
                            Name = "Calc 2",
                            SourceCode = "return 1000"
                        }
                    },
                    new Calculation
                    {
                        Current = new CalculationVersion
                        {
                            Name = "Calc 3",
                            SourceCode = "return Sum(Calc4)"
                        }
                    },
                    new Calculation
                    {
                        Current = new CalculationVersion
                        {
                            Name = "Calc 4",
                            SourceCode = "return 1000"
                        }
                    }
                });

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(
                logger: logger, cacheProvider: cacheProvider,
                jobsApiClient: jobsApiClient, calculationsRepository: calculationsRepository, providersApiClient: providersApiClient);

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            await
                jobsApiClient
                    .DidNotReceive()
                    .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>());

            await
                jobsApiClient
                .Received(1)
                .AddJobLog(Arg.Is(parentJobId), Arg.Is<JobLogUpdateModel>(l => l.CompletedSuccessfully == true && l.Outcome == "Calculations not run as no scoped providers set for specification"));

            logger
                .Received(1)
                .Information(Arg.Is($"No scoped providers set for specification '{specificationId}'"));
        }


        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectAndSummariesInCacheButDoesntMatchScopedProviderIdCountAndUsingServiceBus_CallsRegenerateScopedProviders()
        {
            //Arrange
            EngineSettings engineSettings = CreateEngineSettings();
            engineSettings.MaxPartitionSize = 1;

            IEnumerable<ProviderSummary> providerSummaries = new[]
            {
                new ProviderSummary{ Id = "1" },
                new ProviderSummary{ Id = "2" },
                new ProviderSummary{ Id = "3" },
                new ProviderSummary{ Id = "4" },
                new ProviderSummary{ Id = "5" },
                new ProviderSummary{ Id = "6" },
                new ProviderSummary{ Id = "7" },
                new ProviderSummary{ Id = "8" },
                new ProviderSummary{ Id = "9" },
            };

            string specificationId = "test-spec1";
            string parentJobId = "job-id-1";
            string jobId = "job2";

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", jobId);
            message.UserProperties.Add("specification-id", specificationId);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(cacheKey)
                .Returns(10);

            IEnumerable<string> providerIds = new[] { "1", "3", "2", "4", "5", "8", "7", "6", "9", "10", "11" };

            cacheProvider
                .ListRangeAsync<ProviderSummary>(Arg.Is(cacheKey), Arg.Is(0), Arg.Is(10))
                .Returns(providerSummaries);

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetScopedProviderIds(Arg.Is(specificationId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, providerIds));

            providersApiClient
                .RegenerateProviderSummariesForSpecification(Arg.Is(specificationId), Arg.Is(false))
                .Returns(new ApiResponse<bool>(HttpStatusCode.OK, true));

            ILogger logger = CreateLogger();

            IJobManagement jobManagement = CreateJobManagement();

            JobViewModel parentJob = new JobViewModel
            {
                Id = parentJobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1",
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob
            };

            ApiResponse<JobViewModel> parentJobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            JobViewModel childJob = new JobViewModel
            {
                Id = jobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1",
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob
            };

            ApiResponse<JobViewModel> jobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, childJob);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJobViewModelResponse);
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModelResponse);
            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(new List<Job> { new Job { SpecificationId = specificationId } });
            jobManagement.WaitForJobsToComplete(Arg.Is<IEnumerable<string>>(_ => _.Single() == JobConstants.DefinitionNames.PopulateScopedProvidersJob), specificationId)
                .Returns(true);

            IMessengerService messengerService = CreateServiceBusMessengerService();

            messengerService.ReceiveMessage(Arg.Any<string>(), 
            Arg.Any<Predicate<Job>>(),
            Arg.Any<TimeSpan>())
                .Returns(new Job { CompletionStatus = CompletionStatus.Succeeded });

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(jobsApiClient: jobsApiClient,
                logger: logger, cacheProvider: cacheProvider, providersApiClient: providersApiClient, jobManagement: jobManagement, messengerService: messengerService);

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            await ((IServiceBusService)messengerService)
                .Received(1)
                .CreateSubscription(Arg.Is(ServiceBusConstants.TopicNames.JobNotifications), Arg.Any<string>());

            await ((IServiceBusService)messengerService)
                .Received(1)
                .DeleteSubscription(Arg.Is(ServiceBusConstants.TopicNames.JobNotifications), Arg.Any<string>());

            await
                providersApiClient
                    .Received(1)
                    .RegenerateProviderSummariesForSpecification(Arg.Is(specificationId), Arg.Is(false));
        }


        [TestMethod]
        public async Task UpdateAllocations_GivenBuildProjectAndSummariesInCacheButDoesntMatchScopedProviderIdCount_CallsRegenerateScopedProviders()
        {
            //Arrange
            EngineSettings engineSettings = CreateEngineSettings();
            engineSettings.MaxPartitionSize = 1;

            IEnumerable<ProviderSummary> providerSummaries = new[]
            {
                new ProviderSummary{ Id = "1" },
                new ProviderSummary{ Id = "2" },
                new ProviderSummary{ Id = "3" },
                new ProviderSummary{ Id = "4" },
                new ProviderSummary{ Id = "5" },
                new ProviderSummary{ Id = "6" },
                new ProviderSummary{ Id = "7" },
                new ProviderSummary{ Id = "8" },
                new ProviderSummary{ Id = "9" },
            };

            string specificationId = "test-spec1";
            string parentJobId = "job-id-1";
            string jobId = "job2";

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", jobId);
            message.UserProperties.Add("specification-id", specificationId);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(cacheKey)
                .Returns(10);

            IEnumerable<string> providerIds = new[] { "1", "3", "2", "4", "5", "8", "7", "6", "9", "10", "11" };

            cacheProvider
                .ListRangeAsync<ProviderSummary>(Arg.Is(cacheKey), Arg.Is(0), Arg.Is(10))
                .Returns(providerSummaries);

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetScopedProviderIds(Arg.Is(specificationId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, providerIds));

            providersApiClient
                .RegenerateProviderSummariesForSpecification(Arg.Is(specificationId), Arg.Is(false))
                .Returns(new ApiResponse<bool>(HttpStatusCode.OK, true));

            ILogger logger = CreateLogger();

            IJobManagement jobManagement = CreateJobManagement();

            JobViewModel parentJob = new JobViewModel
            {
                Id = parentJobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1",
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob
            };

            ApiResponse<JobViewModel> parentJobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            JobViewModel childJob = new JobViewModel
            {
                Id = jobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1",
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob
            };

            ApiResponse<JobViewModel> jobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, childJob);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(parentJobId))
                .Returns(parentJobViewModelResponse);
            jobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModelResponse);
            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(new List<Job> { new Job { SpecificationId = specificationId } });
            jobManagement.WaitForJobsToComplete(Arg.Is<IEnumerable<string>>(_ => _.Single() == JobConstants.DefinitionNames.PopulateScopedProvidersJob), specificationId)
                .Returns(true);

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(jobsApiClient: jobsApiClient,
                logger: logger, cacheProvider: cacheProvider, providersApiClient: providersApiClient, jobManagement: jobManagement);

            //Act
            await buildProjectsService.UpdateAllocations(message);

            //Assert
            await
                providersApiClient
                    .Received(1)
                    .RegenerateProviderSummariesForSpecification(Arg.Is(specificationId), Arg.Is(false));
        }

        [TestMethod]
        public async Task UpdateAllocations_GivenRefreshFundingJobIsRunning_JobNotRun()
        {
            //Arrange
            EngineSettings engineSettings = CreateEngineSettings();
            engineSettings.MaxPartitionSize = 1;

            IEnumerable<ProviderSummary> providerSummaries = new[]
            {
                new ProviderSummary{ Id = "1" },
                new ProviderSummary{ Id = "2" },
                new ProviderSummary{ Id = "3" },
                new ProviderSummary{ Id = "4" },
                new ProviderSummary{ Id = "5" },
                new ProviderSummary{ Id = "6" },
                new ProviderSummary{ Id = "7" },
                new ProviderSummary{ Id = "8" },
                new ProviderSummary{ Id = "9" },
                new ProviderSummary{ Id = "10" }
            };

            string parentJobId = "job-id-1";

            string specificationId = "test-spec1";

            JobViewModel parentJob = new JobViewModel
            {
                Id = parentJobId,
                InvokerUserDisplayName = "Username",
                InvokerUserId = "UserId",
                SpecificationId = specificationId,
                CorrelationId = "correlation-id-1"
            };

            ApiResponse<JobViewModel> jobViewModelResponse = new ApiResponse<JobViewModel>(HttpStatusCode.OK, parentJob);

            string cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            BuildProject buildProject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            Message message = new Message(Encoding.UTF8.GetBytes(""));
            message.UserProperties.Add("jobId", "job-id-1");
            message.UserProperties.Add("specification-id", specificationId);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .KeyExists<ProviderSummary>(Arg.Is(cacheKey))
                .Returns(true);

            cacheProvider
                .ListLengthAsync<ProviderSummary>(cacheKey)
                .Returns(10);

            IEnumerable<string> providerIds = providerSummaries.Select(m => m.Id);

            cacheProvider
                .ListRangeAsync<ProviderSummary>(Arg.Is(cacheKey), Arg.Is(0), Arg.Is(10))
                .Returns(providerSummaries);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetJobById(Arg.Is(parentJobId))
                .Returns(jobViewModelResponse);

            jobsApiClient
                .CreateJobs(Arg.Any<IEnumerable<JobCreateModel>>())
                .Returns(CreateJobs());

            ILogger logger = CreateLogger();

            IProvidersApiClient providersApiClient = CreateProvidersApiClient();
            providersApiClient
                .GetScopedProviderIds(Arg.Is(specificationId))
                .Returns(new ApiResponse<IEnumerable<string>>(HttpStatusCode.OK, providerIds));

            ICalculationEngineRunningChecker calculationEngineRunningChecker = CreateCalculationEngineRunningChecker();
            calculationEngineRunningChecker
                .IsCalculationEngineRunning(specificationId, Arg.Any<IEnumerable<string>>())
                .Returns(true);

            IJobManagement jobManagement = CreateJobManagement();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(
                logger: logger, cacheProvider: cacheProvider,
                jobsApiClient: jobsApiClient, engineSettings: engineSettings, providersApiClient: providersApiClient,
                calculationEngineRunningChecker: calculationEngineRunningChecker,
                jobManagement: jobManagement);

            //Act
            Func<Task> test = async () => await buildProjectsService.UpdateAllocations(message);

            //Assert

            test
                .Should()
                .ThrowExactly<NonRetriableException>()
                .WithMessage("Can not create job for specification: test-spec1 as there is an existing Refresh Funding Job running for it. Please wait for that job to finish.");

            await jobManagement
                    .Received(1)
                    .UpdateJobStatus(Arg.Is(parentJobId), Arg.Is<JobLogUpdateModel>(c => c.CompletedSuccessfully == false));
        }

        [TestMethod]
        public async Task CompileAndSaveAssembly_GivenFeatureToggleIsDynamicBuildProjectEnabledIsOff_EnsuresUpdatesCosmos()
        {
            //Arrange
            Build build = new Build()
            {
                Success = true,
            };

            IEnumerable<Calculation> calculations = new[]
            {
                new Calculation()
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                    .Returns(calculations);

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Is(calculations), Arg.Any<CompilerOptions>())
                    .Returns(build);

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsDynamicBuildProjectEnabled()
                    .Returns(false);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectRepository();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(
               calculationsRepository: calculationsRepository,
               sourceCodeService: sourceCodeService,
               buildProjectsRepository: buildProjectsRepository,
               featureToggle: featureToggle);

            //Act
            IActionResult actionResult = await buildProjectsService.CompileAndSaveAssembly(SpecificationId);

            //Assert
            actionResult
                .Should()
                    .BeAssignableTo<NoContentResult>();

            await
                buildProjectsRepository
                    .Received(1)
                    .UpdateBuildProject(Arg.Any<BuildProject>());

            await
                sourceCodeService
                    .Received(1)
                        .SaveAssembly(Arg.Any<BuildProject>());
        }

        [TestMethod]
        public async Task CompileAndSaveAssembly_GivenFeatureToggleIsDynamicBuildProjectEnabledIson_DoesNotUpdateCosmos()
        {
            //Arrange
            Build build = new Build()
            {
                Success = true,
            };

            IEnumerable<Calculation> calculations = new[]
            {
                new Calculation()
            };

            ICalculationsRepository calculationsRepository = CreateCalculationsRepository();
            calculationsRepository
                .GetCalculationsBySpecificationId(Arg.Is(SpecificationId))
                .Returns(calculations);

            ISourceCodeService sourceCodeService = CreateSourceCodeService();
            sourceCodeService
                .Compile(Arg.Any<BuildProject>(), Arg.Is(calculations), Arg.Any<CompilerOptions>())
                .Returns(build);

            IFeatureToggle featureToggle = CreateFeatureToggle();
            featureToggle
                .IsDynamicBuildProjectEnabled()
                .Returns(true);

            IBuildProjectsRepository buildProjectsRepository = CreateBuildProjectRepository();

            BuildProjectsService buildProjectsService = CreateBuildProjectsService(
               calculationsRepository: calculationsRepository,
               sourceCodeService: sourceCodeService,
               buildProjectsRepository: buildProjectsRepository,
               featureToggle: featureToggle);

            //Act
            IActionResult actionResult = await buildProjectsService.CompileAndSaveAssembly(SpecificationId);

            //Assert
            actionResult
                .Should()
                .BeAssignableTo<NoContentResult>();

            await
                buildProjectsRepository
                    .DidNotReceive()
                    .UpdateBuildProject(Arg.Any<BuildProject>());

            await
                sourceCodeService
                    .Received(1)
                    .SaveAssembly(Arg.Any<BuildProject>());
        }

        private IEnumerable<Job> CreateJobs(int count = 10)
        {
            IList<Job> jobs = new List<Job>();

            for (int i = 1; i <= count; i++)
            {
                jobs.Add(new Job
                {
                    Id = $"job-{count}"
                });
            }

            return jobs;
        }

        private static BuildProjectsService CreateBuildProjectsService(
            ILogger logger = null,
            ITelemetry telemetry = null,
            IProvidersApiClient providersApiClient = null,
            ICacheProvider cacheProvider = null,
            ICalculationsRepository calculationsRepository = null,
            IFeatureToggle featureToggle = null,
            IJobsApiClient jobsApiClient = null,
            EngineSettings engineSettings = null,
            ISourceCodeService sourceCodeService = null,
            IDatasetRepository datasetRepository = null,
            IBuildProjectsRepository buildProjectsRepository = null,
            ICalculationEngineRunningChecker calculationEngineRunningChecker = null,
            IJobManagement jobManagement = null,
            IMessengerService messengerService = null,
            IGraphRepository graphRepository = null)
        {
            return new BuildProjectsService(
                logger ?? CreateLogger(),
                telemetry ?? CreateTelemetry(),
                providersApiClient ?? CreateProvidersApiClient(),
                cacheProvider ?? CreateCacheProvider(),
                calculationsRepository ?? CreateCalculationsRepository(),
                featureToggle ?? CreateFeatureToggle(),
                jobsApiClient ?? CreateJobsApiClient(),
                CalcsResilienceTestHelper.GenerateTestPolicies(),
                engineSettings ?? CreateEngineSettings(),
                sourceCodeService ?? CreateSourceCodeService(),
                datasetRepository ?? CreateDatasetRepository(),
                buildProjectsRepository ?? CreateBuildProjectRepository(),
                calculationEngineRunningChecker ?? CreateCalculationEngineRunningChecker(),
                jobManagement ?? CreateJobManagement(),
                messengerService ?? CreateMessengerService(),
                graphRepository ?? CreateGraphRepository());
        }

        private static ISourceCodeService CreateSourceCodeService()
        {
            return Substitute.For<ISourceCodeService>();
        }

        private static IJobManagement CreateJobManagement()
        {
            return Substitute.For<IJobManagement>();
        }

        private static IMessengerService CreateMessengerService()
        {
            return Substitute.For<IMessengerService, IQueueService>();
        }

        private static IMessengerService CreateServiceBusMessengerService()
        {
            return Substitute.For<IMessengerService, IServiceBusService>();
        }

        private static IGraphRepository CreateGraphRepository()
        {
            return Substitute.For<IGraphRepository>();
        }

        private static EngineSettings CreateEngineSettings(int maxPartitionSize = 1000)
        {
            return new EngineSettings
            {
                MaxPartitionSize = maxPartitionSize
            };
        }

        private static IFeatureToggle CreateFeatureToggle()
        {
            IFeatureToggle featureToggle = Substitute.For<IFeatureToggle>();
            featureToggle
                .IsDynamicBuildProjectEnabled()
                .Returns(true);

            return featureToggle;
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

        private static ICalculationsRepository CreateCalculationsRepository()
        {
            return Substitute.For<ICalculationsRepository>();
        }

        private static IJobsApiClient CreateJobsApiClient()
        {
            return Substitute.For<IJobsApiClient>();
        }

        private static IDatasetRepository CreateDatasetRepository()
        {
            return Substitute.For<IDatasetRepository>();
        }

        private static IBuildProjectsRepository CreateBuildProjectRepository()
        {
            return Substitute.For<IBuildProjectsRepository>();
        }

        private static ICalculationEngineRunningChecker CreateCalculationEngineRunningChecker()
        {
            return Substitute.For<ICalculationEngineRunningChecker>();
        }

        private static IProvidersApiClient CreateProvidersApiClient()
        {
            return Substitute.For<IProvidersApiClient>();
        }
    }
}
