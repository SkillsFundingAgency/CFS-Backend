using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Jobs.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Jobs.Services
{
    [TestClass]
    public class JobDefinitionsServiceTests
    {
        private const string jsonFile = "12345.json";

        [TestMethod]
        public async Task SaveDefinition_GivenEmptyJson_ReturnsBadRequest()
        {
            //Arrange

            ILogger logger = CreateLogger();

            JobDefinitionsService jobDefinitionsService = CreateJobDefinitionService(logger: logger);

            //Act
            IActionResult result = await jobDefinitionsService.SaveDefinition(null, jsonFile);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"Invalid json was provided for file: {jsonFile}");

            logger
                .Received(1)
                .Error(Arg.Is($"Null or empty json provided for file: {jsonFile}"));
        }

        [TestMethod]
        public async Task SaveDefinition_GivenInvalidJson_ReturnsBadRequest()
        {
            //Arrange
            string yaml = "invalid json";

            ILogger logger = CreateLogger();

            JobDefinitionsService jobDefinitionsService = CreateJobDefinitionService(logger: logger);

            //Act
            IActionResult result = await jobDefinitionsService.SaveDefinition(yaml, jsonFile);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"Invalid json was provided for file: {jsonFile}");

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Invalid json was provided for file: {jsonFile}"));
        }

        [TestMethod]
        public async Task SaveDefinition_GivenValidJsonButSavingReturns400_ReturnsStatusCodeResult400()
        {
            //Arrange
            string yaml = JsonConvert.SerializeObject(new JobDefinition());

            ILogger logger = CreateLogger();

            IJobDefinitionsRepository jobDefinitionsRepository = CreateJobDefinitionsRepository();
            jobDefinitionsRepository
                .SaveJobDefinition(Arg.Any<JobDefinition>())
                .Returns(HttpStatusCode.BadRequest);

            JobDefinitionsService jobDefinitionsService = CreateJobDefinitionService(logger: logger, jobDefinitionsRepository: jobDefinitionsRepository);

            //Act
            IActionResult result = await jobDefinitionsService.SaveDefinition(yaml, jsonFile);

            //Assert
            result
                .Should()
                .BeOfType<StatusCodeResult>()
                .Which
                .StatusCode
                .Should()
                .Be(400);

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to save json file: {jsonFile} to cosmos db with status 400"));
        }

        [TestMethod]
        public async Task SaveDefinition_GivenValidJsonButSavingThrowsException_ReturnsInternalServerError()
        {
            //Arrange
            string yaml = JsonConvert.SerializeObject(new JobDefinition());

            ILogger logger = CreateLogger();

            IJobDefinitionsRepository jobDefinitionsRepository = CreateJobDefinitionsRepository();
            jobDefinitionsRepository
                .When(x => x.SaveJobDefinition(Arg.Any<JobDefinition>()))
                .Do(x => { throw new Exception(); });


            JobDefinitionsService jobDefinitionsService = CreateJobDefinitionService(logger: logger, jobDefinitionsRepository: jobDefinitionsRepository);

            //Act
            IActionResult result = await jobDefinitionsService.SaveDefinition(yaml, jsonFile);

            //Assert
            result
                .Should()
                .BeOfType<InternalServerErrorResult>()
                .Which
                .Value
                .Should()
                .Be($"Exception occurred writing json file: {jsonFile} to cosmos db");

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is($"Exception occurred writing json file: {jsonFile} to cosmos db"));
        }

        [TestMethod]
        public async Task SaveDefinition_GivenJsonSavesOK_ReturnsNoContentResult()
        {
            //Arrange
            string yaml = JsonConvert.SerializeObject(new JobDefinition());

            ILogger logger = CreateLogger();

            IJobDefinitionsRepository jobDefinitionsRepository = CreateJobDefinitionsRepository();
            jobDefinitionsRepository
                .SaveJobDefinition(Arg.Any<JobDefinition>())
                .Returns(HttpStatusCode.OK);

            ICacheProvider cacheProvider = CreateCacheProvider();

            JobDefinitionsService jobDefinitionsService = CreateJobDefinitionService(logger: logger, jobDefinitionsRepository: jobDefinitionsRepository, cacheProvider: cacheProvider);

            //Act
            IActionResult result = await jobDefinitionsService.SaveDefinition(yaml, jsonFile);

            //Assert
            result
                .Should()
                .BeOfType<NoContentResult>();

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<List<JobDefinition>>(Arg.Is(CacheKeys.JobDefinitions));
        }

        [TestMethod]
        public async Task GetAllJobDefinitions_GivenAlreadyInCache_ReturnsFromCache()
        {
            //Arrange
            List<JobDefinition> jobDefinitions = new List<JobDefinition>
            {
                new JobDefinition()
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<JobDefinition>>(Arg.Is(CacheKeys.JobDefinitions))
                .Returns(jobDefinitions);

            IJobDefinitionsRepository jobDefinitionsRepository = CreateJobDefinitionsRepository();

            JobDefinitionsService jobDefinitionsService = CreateJobDefinitionService(jobDefinitionsRepository, cacheProvider: cacheProvider);

            //Act
            IEnumerable<JobDefinition> definitions = await jobDefinitionsService.GetAllJobDefinitions();

            //Assert
            definitions
                .Count()
                .Should()
                .Be(1);

            await jobDefinitionsRepository
                .DidNotReceive()
                    .GetJobDefinitions();
        }

        [TestMethod]
        public async Task GetAllJobDefinitions_GivenNotInCacheAndNotInCosmos_ReturnsNullListAndDoesNotSetInCache()
        {
            //Arrange
            List<JobDefinition> jobDefinitions = null;

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<JobDefinition>>(Arg.Is(CacheKeys.JobDefinitions))
                .Returns(jobDefinitions);

            IJobDefinitionsRepository jobDefinitionsRepository = CreateJobDefinitionsRepository();
            jobDefinitionsRepository
               .GetJobDefinitions()
               .Returns(jobDefinitions);

            JobDefinitionsService jobDefinitionsService = CreateJobDefinitionService(jobDefinitionsRepository, resiliencePolicies: GenerateTestPolicies(), cacheProvider: cacheProvider);

            //Act
            IEnumerable<JobDefinition> definitions = await jobDefinitionsService.GetAllJobDefinitions();

            //Assert
            definitions
                .Should()
                .BeNull();

            await
                cacheProvider
                    .DidNotReceive()
                    .SetAsync(Arg.Any<string>(), Arg.Any<List<JobDefinition>>());
        }

        [TestMethod]
        public async Task GetAllJobDefinitions_GivenNotInCacheButFoundInCosmoshe_ReturnsAndSetsInCache()
        {
            //Arrange
            List<JobDefinition> jobDefinitions = new List<JobDefinition>
            {
                new JobDefinition()
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<JobDefinition>>(Arg.Is(CacheKeys.JobDefinitions))
                .Returns((List<JobDefinition>)null);

            IJobDefinitionsRepository jobDefinitionsRepository = CreateJobDefinitionsRepository();
            jobDefinitionsRepository
                .GetJobDefinitions()
                .Returns(jobDefinitions);

            JobDefinitionsService jobDefinitionsService = CreateJobDefinitionService(jobDefinitionsRepository, resiliencePolicies: GenerateTestPolicies(), cacheProvider: cacheProvider);

            //Act
            IEnumerable<JobDefinition> definitions = await jobDefinitionsService.GetAllJobDefinitions();

            //Assert
            definitions
                .Count()
                .Should()
                .Be(1);

            await
                cacheProvider
                    .Received(1)
                    .SetAsync(Arg.Is(CacheKeys.JobDefinitions), Arg.Any<List<JobDefinition>>());
        }

        [TestMethod]
        public async Task GetJobDefinitions_GivenNotInCacheAndNotInCosmos_ReturnsNullListAndDoesNotSetInCache()
        {
            //Arrange
            List<JobDefinition> jobDefinitions = null;

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<JobDefinition>>(Arg.Is(CacheKeys.JobDefinitions))
                .Returns(jobDefinitions);

            IJobDefinitionsRepository jobDefinitionsRepository = CreateJobDefinitionsRepository();
            jobDefinitionsRepository
               .GetJobDefinitions()
               .Returns(jobDefinitions);

            JobDefinitionsService jobDefinitionsService = CreateJobDefinitionService(jobDefinitionsRepository, resiliencePolicies: GenerateTestPolicies(), cacheProvider: cacheProvider);

            //Act
            IActionResult result = await jobDefinitionsService.GetJobDefinitions();

            //Assert
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be("No job definitions were found");
        }

        [TestMethod]
        public async Task GetJobDefinitions_GivenResultsFoundAndNotInCosmos_ReturnsOKObjectResultotSetInCache()
        {
            //Arrange
            List<JobDefinition> jobDefinitions = new List<JobDefinition>
            {
                new JobDefinition()
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<JobDefinition>>(Arg.Is(CacheKeys.JobDefinitions))
                .Returns(jobDefinitions);

            JobDefinitionsService jobDefinitionsService = CreateJobDefinitionService(cacheProvider: cacheProvider);

            //Act
            IActionResult result = await jobDefinitionsService.GetJobDefinitions();

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo(jobDefinitions);
        }

        [TestMethod]
        public async Task GetJobDefinitionById_GivenInvalidDefinitionId_ReturnsBadRequestResult()
        {
            //Arrange
            const string jobDefinitionId = "";

            JobDefinitionsService jobDefinitionsService = CreateJobDefinitionService();

            //Act
            IActionResult result = await jobDefinitionsService.GetJobDefinitionById(jobDefinitionId);

            //Assert
            result
                .Should()
                .BeAssignableTo<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .Be("Job definition id was not provid");
        }

        [TestMethod]
        public async Task GetJobDefinitionById_GivenDefinitionNotFound_ReturnsNotFoundResult()
        {
            //Arrange
            const string jobDefinitionId = "jobdef-1";

            List<JobDefinition> jobDefinitions = null;

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<JobDefinition>>(Arg.Is(CacheKeys.JobDefinitions))
                .Returns(jobDefinitions);

            JobDefinitionsService jobDefinitionsService = CreateJobDefinitionService(cacheProvider: cacheProvider, resiliencePolicies: GenerateTestPolicies());

            //Act
            IActionResult result = await jobDefinitionsService.GetJobDefinitionById(jobDefinitionId);

            //Assert
            result
                .Should()
                .BeOfType<NotFoundObjectResult>()
                .Which
                .Value
                .Should()
                .Be($"No job definitions were found for id {jobDefinitionId}");
        }

        [TestMethod]
        public async Task GetJobDefinitionById_GivenDefinitionFound_ReturnsOKObjectResult()
        {
            //Arrange
            const string jobDefinitionId = "jobdef-1";

            List<JobDefinition> jobDefinitions = new List<JobDefinition>
            {
                new JobDefinition
                {
                    Id = jobDefinitionId
                }
            };

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<JobDefinition>>(Arg.Is(CacheKeys.JobDefinitions))
                .Returns(jobDefinitions);

            JobDefinitionsService jobDefinitionsService = CreateJobDefinitionService(cacheProvider: cacheProvider);

            //Act
            IActionResult result = await jobDefinitionsService.GetJobDefinitionById(jobDefinitionId);

            //Assert
            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeEquivalentTo(jobDefinitions.First());
        }

        public JobDefinitionsService CreateJobDefinitionService(
            IJobDefinitionsRepository jobDefinitionsRepository = null,
            ILogger logger = null,
            IJobsResiliencePolicies resiliencePolicies = null,
            ICacheProvider cacheProvider = null)
        {
            return new JobDefinitionsService(
                    jobDefinitionsRepository ?? CreateJobDefinitionsRepository(),
                    logger ?? CreateLogger(),
                    resiliencePolicies ?? JobsResilienceTestHelper.GenerateTestPolicies(),
                    cacheProvider ?? CreateCacheProvider()
                );
        }

        public static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        public IJobDefinitionsRepository CreateJobDefinitionsRepository()
        {
            return Substitute.For<IJobDefinitionsRepository>();
        }

        public static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        public static IJobsResiliencePolicies GenerateTestPolicies()
        {
            return new ResiliencePolicies()
            {
                JobDefinitionsRepository = Policy.NoOpAsync(),
                CacheProviderPolicy = Policy.NoOpAsync()
            };
        }
    }
}
