using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using CalculateFunding.Services.CosmosDbScaling.Repositories;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CosmosDbScaling
{
    [TestClass]
    public class CosmosDbScalingServiceTests
    {
        [TestMethod]
        public void Scaleup_GivenNoPayloadProvided_ThrowsNonRetriableException()
        {
            //Arrange
            Message message = new Message();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService();

            //Act
            Func<Task> test = async () => await cosmosDbScalingService.ScaleUp(message);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void ScaleUp_GivenMessageWithOneRepositoryTypeButNoConfigReturned_ThrowsRetriableexception()
        {
            //Arrange
            JobNotification jobNotification = new JobNotification
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.Queued
            };

            CosmosDbScalingRequestModel requestModel = new CosmosDbScalingRequestModel
            {
                RepositoryTypes = new[]
                {
                    CosmosRepositoryType.CalculationProviderResults
                },
                JobDefinitionId = "job-def-1"
            };

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();
            modelBuilder
                .BuildRequestModel(Arg.Any<JobNotification>())
                .Returns(requestModel);

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(cosmosDbScalingRequestModelBuilder: modelBuilder);

            //Act
            Func<Task> test = async () => await cosmosDbScalingService.ScaleUp(message);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be("Failed to increase cosmosdb request units");
        }

        [TestMethod]
        public async Task ScaleUp_WhenNotificationIsInProgress_DoesNotBuildRequestModel()
        {
            //Arrange
            JobNotification jobNotification = new JobNotification
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.InProgress
            };

            string json = JsonConvert.SerializeObject(jobNotification);

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();

            Message message = new Message(Encoding.UTF8.GetBytes(json));
           

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                cosmosDbScalingRequestModelBuilder: modelBuilder);

            //Act
            await cosmosDbScalingService.ScaleUp(message);

            //Assert
            modelBuilder
                .DidNotReceive()
                .BuildRequestModel(Arg.Any<JobNotification>());
        }

        [TestMethod]
        public void ScaleUp_GivenMessageWithOneRepositoryTypeButFailedToSetThroughPut_ThrowsRetriableException()
        {
            //Arrange
            JobNotification jobNotification = new JobNotification
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.Queued
            };

            CosmosDbScalingRequestModel requestModel = new CosmosDbScalingRequestModel
            {
                RepositoryTypes = new[]
               {
                    CosmosRepositoryType.CalculationProviderResults
                },
                JobDefinitionId = "job-def-1"
            };

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();
            modelBuilder
                .BuildRequestModel(Arg.Any<JobNotification>())
                .Returns(requestModel);

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosRepositoryType.CalculationProviderResults);

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingConfig);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();
            scalingRepository
                .When(x => x.SetThroughput(Arg.Any<int>()))
                .Do(x => { throw new Exception(); });

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingRepository);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider,
                cosmosDbScalingRequestModelBuilder: modelBuilder);

            //Act
            Func<Task> test = async () => await cosmosDbScalingService.ScaleUp(message);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be("Failed to increase cosmosdb request units");

            logger
               .Received(1)
               .Error(Arg.Any<Exception>(), Arg.Is($"Failed to set throughput on repository type '{scalingConfig.RepositoryType}' with '{scalingConfig.JobRequestUnitConfigs.First().JobRequestUnits}' request units"));
        }

        [TestMethod]
        public void ScaleUp_GivenMessageWithOneRepositoryTypeButfailedToSaveCurrentRequestUnits_ThrowsRetriableException()
        {
            //Arrange
            JobNotification jobNotification = new JobNotification
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.Queued
            };

            CosmosDbScalingRequestModel requestModel = new CosmosDbScalingRequestModel
            {
                RepositoryTypes = new[]
                {
                    CosmosRepositoryType.CalculationProviderResults
                },
                JobDefinitionId = "job-def-1"
            };

            string json = JsonConvert.SerializeObject(jobNotification);

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();
            modelBuilder
                .BuildRequestModel(Arg.Any<JobNotification>())
                .Returns(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosRepositoryType.CalculationProviderResults);

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingConfig);
            cosmosDbScalingConfigRepository
                .UpdateCurrentRequestUnits(Arg.Any<CosmosDbScalingConfig>())
                .Returns(HttpStatusCode.BadRequest);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();
           
            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingRepository);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider,
                cosmosDbScalingRequestModelBuilder: modelBuilder);

            //Act
            Func<Task> test = async () => await cosmosDbScalingService.ScaleUp(message);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be("Failed to increase cosmosdb request units");

            logger
                .Received(1)
                .Error($"Failed to update cosmos scale config repository type: '{scalingConfig.RepositoryType}' with new request units of '{scalingConfig.CurrentRequestUnits}' with status code: '{HttpStatusCode.BadRequest}'");
        }

        [TestMethod]
        public async Task ScaleUp_WhenSuccessfullySetsThroughputAndUpdatesConfig_InvalidatesCache()
        {
            //Arrange
            JobNotification jobNotification = new JobNotification
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.Queued
            };

            CosmosDbScalingRequestModel requestModel = new CosmosDbScalingRequestModel
            {
                RepositoryTypes = new[]
                {
                    CosmosRepositoryType.CalculationProviderResults
                },
                JobDefinitionId = "job-def-1"
            };

            string json = JsonConvert.SerializeObject(jobNotification);

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();
            modelBuilder
                .BuildRequestModel(Arg.Any<JobNotification>())
                .Returns(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosRepositoryType.CalculationProviderResults);

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingConfig);
            cosmosDbScalingConfigRepository
                .UpdateCurrentRequestUnits(Arg.Any<CosmosDbScalingConfig>())
                .Returns(HttpStatusCode.OK);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingRepository);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider,
                cacheProvider: cacheProvider,
                cosmosDbScalingRequestModelBuilder: modelBuilder);

            //Act
            await cosmosDbScalingService.ScaleUp(message);

            //Assert
            await
                cacheProvider
                    .Received(1)
                    .RemoveAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs));
        }

        [TestMethod]
        public async Task ScaleUp_WhenCurrentRequestUnitsNotAtBaseLine_EnsuresAddsToCurrentRequestUnits()
        {
            //Arrange
            JobNotification jobNotification = new JobNotification
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.Queued
            };

            CosmosDbScalingRequestModel requestModel = new CosmosDbScalingRequestModel
            {
                RepositoryTypes = new[]
                {
                    CosmosRepositoryType.CalculationProviderResults
                },
                JobDefinitionId = "job-def-1"
            };

            string json = JsonConvert.SerializeObject(jobNotification);

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();
            modelBuilder
                .BuildRequestModel(Arg.Any<JobNotification>())
                .Returns(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosRepositoryType.CalculationProviderResults);
            scalingConfig.CurrentRequestUnits = 100000;

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingConfig);
            cosmosDbScalingConfigRepository
                .UpdateCurrentRequestUnits(Arg.Any<CosmosDbScalingConfig>())
                .Returns(HttpStatusCode.OK);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingRepository);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider,
                cosmosDbScalingRequestModelBuilder: modelBuilder);

            //Act
            await cosmosDbScalingService.ScaleUp(message);

            //Assert
            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Is(150000));

            await
                cosmosDbScalingConfigRepository
                .Received(1)
                .UpdateCurrentRequestUnits(Arg.Is<CosmosDbScalingConfig>(m => m.CurrentRequestUnits == 150000));
        }

        [TestMethod]
        public async Task ScaleUp_WhenCurrentRequestUnitsAreAt180000_EnsuresDoesntExceedMaximum()
        {
            //Arrange
            JobNotification jobNotification = new JobNotification
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.Queued
            };

            CosmosDbScalingRequestModel requestModel = new CosmosDbScalingRequestModel
            {
                RepositoryTypes = new[]
               {
                    CosmosRepositoryType.CalculationProviderResults
                },
                JobDefinitionId = "job-def-1"
            };

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosRepositoryType.CalculationProviderResults);
            scalingConfig.CurrentRequestUnits = 180000;

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingConfig);
            cosmosDbScalingConfigRepository
                .UpdateCurrentRequestUnits(Arg.Any<CosmosDbScalingConfig>())
                .Returns(HttpStatusCode.OK);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();
            modelBuilder
                .BuildRequestModel(Arg.Any<JobNotification>())
                .Returns(requestModel);

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingRepository);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider,
                cosmosDbScalingRequestModelBuilder: modelBuilder);

            //Act
            await cosmosDbScalingService.ScaleUp(message);

            //Assert
            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Is(scalingConfig.MaxRequestUnits));

            await
                cosmosDbScalingConfigRepository
                .Received(1)
                .UpdateCurrentRequestUnits(Arg.Is<CosmosDbScalingConfig>(m => m.CurrentRequestUnits == scalingConfig.MaxRequestUnits));
        }

        [TestMethod]
        public async Task ScaleUp_WhenCurrentRequestUnitsAreAtMaxium_EnsuresDoesntExceedMaximum()
        {
            //Arrange
            JobNotification jobNotification = new JobNotification
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.Queued
            };

            CosmosDbScalingRequestModel requestModel = new CosmosDbScalingRequestModel
            {
                RepositoryTypes = new[]
               {
                    CosmosRepositoryType.CalculationProviderResults
                },
                JobDefinitionId = "job-def-1"
            };

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosRepositoryType.CalculationProviderResults);
            scalingConfig.CurrentRequestUnits = scalingConfig.MaxRequestUnits;

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingConfig);
            cosmosDbScalingConfigRepository
                .UpdateCurrentRequestUnits(Arg.Any<CosmosDbScalingConfig>())
                .Returns(HttpStatusCode.OK);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingRepository);

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();
            modelBuilder
                .BuildRequestModel(Arg.Any<JobNotification>())
                .Returns(requestModel);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider,
                cosmosDbScalingRequestModelBuilder: modelBuilder);

            //Act
            await cosmosDbScalingService.ScaleUp(message);

            //Assert
            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Is(scalingConfig.MaxRequestUnits));

            await
                cosmosDbScalingConfigRepository
                .Received(1)
                .UpdateCurrentRequestUnits(Arg.Is<CosmosDbScalingConfig>(m => m.CurrentRequestUnits == scalingConfig.MaxRequestUnits));
        }

        [TestMethod]
        public void ScaleDown_WhenFailingToFecthJobSummaries_ThrowsNewRetriableException()
        {
            //Arrange
            ApiResponse<IEnumerable<JobSummary>> jobSummariesResponse = new ApiResponse<IEnumerable<JobSummary>>(HttpStatusCode.BadRequest);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummariesResponse);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(logger, jobsApiClient: jobsApiClient);

            //Act
            Func<Task> test = async () => await cosmosDbScalingService.ScaleDown();

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be("Failed to fetch job summaries that are still running within the last hour");

            logger
                .Received(1)
                .Error("Failed to fetch job summaries that are still running within the last hour");
        }

        [TestMethod]
        public async Task ScaleDown_WhenJobSummariesReturnedAndConfigsReturnedFromCacheButConfigAlreadyAtBaseline_DoesNotUpdateConfig()
        {
            //Arrange
            IEnumerable<JobSummary> jobSummaries = new[]
            {
                new JobSummary
                {
                    JobType = "job-def-1"
                }
            };

            CosmosDbScalingConfig cosmosDbScalingConfig = CreateCosmosScalingConfig(CosmosRepositoryType.CalculationProviderResults);

            IEnumerable<CosmosDbScalingConfig> configs = new[] { cosmosDbScalingConfig };

            ApiResponse<IEnumerable<JobSummary>> jobSummariesResponse = new ApiResponse<IEnumerable<JobSummary>>(HttpStatusCode.OK, jobSummaries);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummariesResponse);

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger, 
                jobsApiClient: jobsApiClient,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider);

            //Act
            await cosmosDbScalingService.ScaleDown();

            //Assert
            await
                cosmosDbScalingConfigRepository
                .DidNotReceive()
                .UpdateCurrentRequestUnits(Arg.Any<CosmosDbScalingConfig>());
        }

        [TestMethod]
        public void ScaleDown_WhenJobSummariesReturnedAndConfigsReturnedFromCacheButFailsToSetThroughput_ThrowsRetriableException()
        {
            //Arrange
            IEnumerable<JobSummary> jobSummaries = new[]
            {
                new JobSummary
                {
                    JobType = "job-def-1"
                }
            };

            CosmosDbScalingConfig cosmosDbScalingConfig = CreateCosmosScalingConfig(CosmosRepositoryType.CalculationProviderResults);
            cosmosDbScalingConfig.CurrentRequestUnits = cosmosDbScalingConfig.MaxRequestUnits;

            IEnumerable<CosmosDbScalingConfig> configs = new[] { cosmosDbScalingConfig };

            ApiResponse<IEnumerable<JobSummary>> jobSummariesResponse = new ApiResponse<IEnumerable<JobSummary>>(HttpStatusCode.OK, jobSummaries);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummariesResponse);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();
            scalingRepository
               .When(x => x.SetThroughput(Arg.Any<int>()))
               .Do(x => { throw new Exception(); });

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                jobsApiClient: jobsApiClient,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            Func<Task> test = async() => await cosmosDbScalingService.ScaleDown();

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Failed to scale down collection for repository type '{cosmosDbScalingConfig.RepositoryType}'");
        }

        [TestMethod]
        public async Task ScaleDown_WhenJobSummariesReturnedAndConfigsReturnedFromCacheButFailsToUpdateConfig_ThrowsRetriableException()
        {
            //Arrange
            IEnumerable<JobSummary> jobSummaries = new[]
            {
                new JobSummary
                {
                    JobType = "job-def-1"
                }
            };

            CosmosDbScalingConfig cosmosDbScalingConfig = CreateCosmosScalingConfig(CosmosRepositoryType.CalculationProviderResults);
            cosmosDbScalingConfig.CurrentRequestUnits = cosmosDbScalingConfig.MaxRequestUnits;

            IEnumerable<CosmosDbScalingConfig> configs = new[] { cosmosDbScalingConfig };

            ApiResponse<IEnumerable<JobSummary>> jobSummariesResponse = new ApiResponse<IEnumerable<JobSummary>>(HttpStatusCode.OK, jobSummaries);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummariesResponse);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .UpdateCurrentRequestUnits(Arg.Any<CosmosDbScalingConfig>())
                .Returns(HttpStatusCode.BadRequest);

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();
           
            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                jobsApiClient: jobsApiClient,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            Func<Task> test = async () => await cosmosDbScalingService.ScaleDown();

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Failed to scale down collection for repository type '{cosmosDbScalingConfig.RepositoryType}'");

            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Is(cosmosDbScalingConfig.BaseRequestUnits));
        }

        [TestMethod]
        public async Task ScaleDown_WhenJobSummariesReturnedAndConfigsReturnedFromCacheButAlreadyAtBaseLine_DoesNotSetThroughput()
        {
            //Arrange
            IEnumerable<JobSummary> jobSummaries = new[]
            {
                new JobSummary
                {
                    JobType = "job-def-1"
                }
            };

            CosmosDbScalingConfig cosmosDbScalingConfig = CreateCosmosScalingConfig(CosmosRepositoryType.CalculationProviderResults);
            cosmosDbScalingConfig.CurrentRequestUnits = cosmosDbScalingConfig.BaseRequestUnits;

            IEnumerable<CosmosDbScalingConfig> configs = new[] { cosmosDbScalingConfig };

            ApiResponse<IEnumerable<JobSummary>> jobSummariesResponse = new ApiResponse<IEnumerable<JobSummary>>(HttpStatusCode.OK, jobSummaries);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummariesResponse);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                jobsApiClient: jobsApiClient,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            await cosmosDbScalingService.ScaleDown();

           //Assert
            await
                scalingRepository
                .DidNotReceive()
                .SetThroughput(Arg.Any<int>());
        }

        [TestMethod]
        public async Task ScaleDown_WhenJobSummariesReturnedAndConfigsReturnedFromCacheAndCurrentRequestsNotAtBaseline_SetsThroughputAndUpdatesConfig()
        {
            //Arrange
            IEnumerable<JobSummary> jobSummaries = new[]
            {
                new JobSummary
                {
                    JobType = "job-def-1"
                }
            };

            CosmosDbScalingConfig cosmosDbScalingConfig = CreateCosmosScalingConfig(CosmosRepositoryType.CalculationProviderResults);
            cosmosDbScalingConfig.CurrentRequestUnits = cosmosDbScalingConfig.MaxRequestUnits;

            IEnumerable<CosmosDbScalingConfig> configs = new[] { cosmosDbScalingConfig };

            ApiResponse<IEnumerable<JobSummary>> jobSummariesResponse = new ApiResponse<IEnumerable<JobSummary>>(HttpStatusCode.OK, jobSummaries);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummariesResponse);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
               .UpdateCurrentRequestUnits(Arg.Any<CosmosDbScalingConfig>())
               .Returns(HttpStatusCode.OK);

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                jobsApiClient: jobsApiClient,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            await cosmosDbScalingService.ScaleDown();

            //Assert
            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Is(cosmosDbScalingConfig.BaseRequestUnits));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs));
        }

        [TestMethod]
        public async Task ScaleDown_WhenJobSummariesReturnedAndConfigsReturnedFromCacheAndCurrentRequestsButAllAtBaseline_DoesNotSetThroughputs()
        {
            //Arrange
            IEnumerable<JobSummary> jobSummaries = new[]
            {
                new JobSummary
                {
                    JobType = "job-def-1"
                },
                new JobSummary
                {
                    JobType = "job-def-2"
                },
                new JobSummary
                {
                    JobType = "job-def-3"
                }
            };

            IEnumerable<CosmosDbScalingConfig> configs = CreateCosmosScalingConfigs();

            ApiResponse<IEnumerable<JobSummary>> jobSummariesResponse = new ApiResponse<IEnumerable<JobSummary>>(HttpStatusCode.OK, jobSummaries);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummariesResponse);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
               .UpdateCurrentRequestUnits(Arg.Any<CosmosDbScalingConfig>())
               .Returns(HttpStatusCode.OK);

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                jobsApiClient: jobsApiClient,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            await cosmosDbScalingService.ScaleDown();

            //Assert
            await
                scalingRepository
                .DidNotReceive()
                .SetThroughput(Arg.Any<int>());
        }

        [TestMethod]
        public async Task ScaleDown_WhenJobSummariesReturnedAndConfigsReturnedFromCacheAndCurrentRequestsButAllOneNotAtBaseline_SetsThroughputForOne()
        {
            //Arrange
            IEnumerable<JobSummary> jobSummaries = new[]
            {
                new JobSummary
                {
                    JobType = "job-def-1"
                },
                new JobSummary
                {
                    JobType = "job-def-2"
                },
                new JobSummary
                {
                    JobType = "job-def-3"
                }
            };

            IEnumerable<CosmosDbScalingConfig> configs = CreateCosmosScalingConfigs();
            configs.First().CurrentRequestUnits = 50000;

            ApiResponse<IEnumerable<JobSummary>> jobSummariesResponse = new ApiResponse<IEnumerable<JobSummary>>(HttpStatusCode.OK, jobSummaries);

            IJobsApiClient jobsApiClient = CreateJobsApiClient();
            jobsApiClient
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummariesResponse);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
               .UpdateCurrentRequestUnits(Arg.Any<CosmosDbScalingConfig>())
               .Returns(HttpStatusCode.OK);

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosRepositoryType.CalculationProviderResults))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                jobsApiClient: jobsApiClient,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            await cosmosDbScalingService.ScaleDown();

            //Assert
            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Any<int>());
        }

        private CosmosDbScalingService CreateScalingService(
            ILogger logger = null,
            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = null,
            IJobsApiClient jobsApiClient = null,
            ICacheProvider cacheProvider = null,
            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = null,
            ICosmosDbScalingRequestModelBuilder cosmosDbScalingRequestModelBuilder = null)
        {
            return new CosmosDbScalingService(
                logger ?? CreateLogger(),
                cosmosDbScalingRepositoryProvider ?? CreateCosmosDbScalingRepositoryProvider(),
                jobsApiClient ?? CreateJobsApiClient(),
                cacheProvider ?? CreateCacheProvider(),
                cosmosDbScalingConfigRepository ?? CreateCosmosDbScalingConfigRepository(),
                CosmosDbScalingResilienceTestHelper.GenerateTestPolicies(),
                cosmosDbScalingRequestModelBuilder ?? CreateReqestModelBuilder());
        }

        private static ICosmosDbScalingRequestModelBuilder CreateReqestModelBuilder()
        {
            return Substitute.For<ICosmosDbScalingRequestModelBuilder>();
        }

        private static ILogger CreateLogger()
        {
            return Substitute.For<ILogger>();
        }

        private static ICosmosDbScalingRepositoryProvider CreateCosmosDbScalingRepositoryProvider()
        {
            return Substitute.For<ICosmosDbScalingRepositoryProvider>();
        }

        private static ICacheProvider CreateCacheProvider()
        {
            return Substitute.For<ICacheProvider>();
        }

        private static ICosmosDbScalingConfigRepository CreateCosmosDbScalingConfigRepository()
        {
            return Substitute.For<ICosmosDbScalingConfigRepository>();
        }

        private static IJobsApiClient CreateJobsApiClient()
        {
            return Substitute.For<IJobsApiClient>();
        }

        private static ICosmosDbScalingRepository CreateCosmosDbScalingRepository()
        {
            return Substitute.For<ICosmosDbScalingRepository>();
        }

        private static CosmosDbScalingConfig CreateCosmosScalingConfig(CosmosRepositoryType cosmosRepositoryType)
        {
            return new CosmosDbScalingConfig
            {
                RepositoryType = cosmosRepositoryType,
                BaseRequestUnits = 10000,
                MaxRequestUnits = 200000,
                CurrentRequestUnits = 10000,
                Id = "1",
                JobRequestUnitConfigs = new[]
                {
                    new CosmosDbScalingJobConfig
                    {
                        JobDefinitionId = "job-def-1",
                        JobRequestUnits = 50000
                    }
                }
            };
        }

        private static IEnumerable<CosmosDbScalingConfig> CreateCosmosScalingConfigs()
        {
            return new[]
            {
                new CosmosDbScalingConfig
                {
                    RepositoryType = CosmosRepositoryType.CalculationProviderResults,
                    BaseRequestUnits = 10000,
                    MaxRequestUnits = 200000,
                    CurrentRequestUnits = 10000,
                    Id = "1",
                    JobRequestUnitConfigs = new[]
                    {
                        new CosmosDbScalingJobConfig
                        {
                            JobDefinitionId = "job-def-1",
                            JobRequestUnits = 50000
                        }
                    }
                },
                new CosmosDbScalingConfig
                {
                    RepositoryType = CosmosRepositoryType.ProviderSourceDatasets,
                    BaseRequestUnits = 2000,
                    MaxRequestUnits = 50000,
                    CurrentRequestUnits = 2000,
                    Id = "1",
                    JobRequestUnitConfigs = new[]
                    {
                        new CosmosDbScalingJobConfig
                        {
                            JobDefinitionId = "job-def-2",
                            JobRequestUnits = 10000
                        }
                    }
                },
                new CosmosDbScalingConfig
                {
                    RepositoryType = CosmosRepositoryType.PublishedProviderResults,
                    BaseRequestUnits = 5000,
                    MaxRequestUnits = 100000,
                    CurrentRequestUnits = 5000,
                    Id = "1",
                    JobRequestUnitConfigs = new[]
                    {
                        new CosmosDbScalingJobConfig
                        {
                            JobDefinitionId = "job-def-3",
                            JobRequestUnits = 20000
                        }
                    }
                }
            };
        }
    }
}
