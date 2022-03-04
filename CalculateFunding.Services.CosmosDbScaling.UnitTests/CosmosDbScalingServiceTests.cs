using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using CalculateFunding.Services.Core.Interfaces.Logging;
using Azure.Messaging.EventHubs;

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
            Func<Task> test = async () => await cosmosDbScalingService.Process(message);

            //Assert
            test
                .Should()
                .ThrowExactly<ArgumentNullException>();
        }

        [TestMethod]
        public void ScaleUp_GivenMessageWithOneRepositoryTypeButNoConfigReturned_ThrowsRetriableexception()
        {
            //Arrange
            JobSummary jobNotification = new JobSummary
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.Queued
            };

            CosmosDbScalingRequestModel requestModel = new CosmosDbScalingRequestModel
            {
                RepositoryTypes = new[]
                {
                    CosmosCollectionType.CalculationProviderResults
                },
                JobDefinitionId = "job-def-1"
            };

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();
            modelBuilder
                .BuildRequestModel(Arg.Any<JobSummary>())
                .Returns(requestModel);

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(cosmosDbScalingRequestModelBuilder: modelBuilder);

            //Act
            Func<Task> test = async () => await cosmosDbScalingService.Process(message);

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
            JobSummary jobNotification = new JobSummary
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
            await cosmosDbScalingService.Process(message);

            //Assert
            modelBuilder
                .DidNotReceive()
                .BuildRequestModel(Arg.Any<JobSummary>());
        }

        [TestMethod]
        public void ScaleUp_GivenMessageWithOneRepositoryTypeButFailedToSetThroughPut_ThrowsRetriableException()
        {
            //Arrange
            JobSummary jobNotification = new JobSummary
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.Queued
            };

            CosmosDbScalingRequestModel requestModel = new CosmosDbScalingRequestModel
            {
                RepositoryTypes = new[]
               {
                    CosmosCollectionType.CalculationProviderResults
                },
                JobDefinitionId = "job-def-1"
            };

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();
            modelBuilder
                .BuildRequestModel(Arg.Any<JobSummary>())
                .Returns(requestModel);

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.CalculationProviderResults);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingConfig);

            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(settings);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();
            scalingRepository
                .When(x => x.SetThroughput(Arg.Any<int>()))
                .Do(x => { throw new Exception(); });

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider,
                cosmosDbScalingRequestModelBuilder: modelBuilder);

            //Act
            Func<Task> test = async () => await cosmosDbScalingService.Process(message);

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
               .Error(Arg.Any<Exception>(), Arg.Is($"Failed to set throughput on repository type '{scalingConfig.RepositoryType}' with '{settings.CurrentRequestUnits}' request units"));
        }

        [TestMethod]
        public void ScaleUp_GivenMessageWithOneRepositoryTypeButfailedToSaveCurrentRequestUnits_ThrowsRetriableException()
        {
            //Arrange
            JobSummary jobNotification = new JobSummary
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.Queued
            };

            CosmosDbScalingRequestModel requestModel = new CosmosDbScalingRequestModel
            {
                RepositoryTypes = new[]
                {
                    CosmosCollectionType.CalculationProviderResults
                },
                JobDefinitionId = "job-def-1"
            };

            string json = JsonConvert.SerializeObject(jobNotification);

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();
            modelBuilder
                .BuildRequestModel(Arg.Any<JobSummary>())
                .Returns(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.CalculationProviderResults);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingConfig);
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(settings);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();
            scalingRepository
                .SetThroughput(Arg.Any<int>())
                .Throws(new Exception());

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider,
                cosmosDbScalingRequestModelBuilder: modelBuilder);

            //Act
            Func<Task> test = async () => await cosmosDbScalingService.Process(message);

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
                .Error(Arg.Any<Exception>(), $"Failed to set throughput on repository type '{settings.CosmosCollectionType}' with '{settings.CurrentRequestUnits}' request units");
        }

        [TestMethod]
        public async Task ScaleUp_WhenSuccessfullySetsThroughputAndUpdatesConfig_InvalidatesCache()
        {
            //Arrange
            JobSummary jobNotification = new JobSummary
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.Queued
            };

            CosmosDbScalingRequestModel requestModel = new CosmosDbScalingRequestModel
            {
                RepositoryTypes = new[]
                {
                    CosmosCollectionType.CalculationProviderResults
                },
                JobDefinitionId = "job-def-1"
            };

            string json = JsonConvert.SerializeObject(jobNotification);

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();
            modelBuilder
                .BuildRequestModel(Arg.Any<JobSummary>())
                .Returns(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));
            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.CalculationProviderResults);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingConfig);
            cosmosDbScalingConfigRepository
                .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
                .Returns(HttpStatusCode.OK);
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(settings);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
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
            await cosmosDbScalingService.Process(message);

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
            JobSummary jobNotification = new JobSummary
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.Queued
            };

            CosmosDbScalingRequestModel requestModel = new CosmosDbScalingRequestModel
            {
                RepositoryTypes = new[]
                {
                    CosmosCollectionType.CalculationProviderResults
                },
                JobDefinitionId = "job-def-1"
            };

            string json = JsonConvert.SerializeObject(jobNotification);

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();
            modelBuilder
                .BuildRequestModel(Arg.Any<JobSummary>())
                .Returns(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.CalculationProviderResults);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            settings.CurrentRequestUnits = 100000;

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingConfig);
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(settings);
            cosmosDbScalingConfigRepository
                .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
                .Returns(HttpStatusCode.OK);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider,
                cosmosDbScalingRequestModelBuilder: modelBuilder);

            //Act
            await cosmosDbScalingService.Process(message);

            //Assert
            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Is(150000));

            await
                cosmosDbScalingConfigRepository
                .Received(1)
                .UpdateCollectionSettings(Arg.Is<CosmosDbScalingCollectionSettings>(m => m.CurrentRequestUnits == 150000));
        }

        [TestMethod]
        public async Task ScaleUp_WhenCurrentRequestUnitsAreAt180000_EnsuresDoesntExceedMaximum()
        {
            //Arrange
            JobSummary jobNotification = new JobSummary
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.Queued
            };

            CosmosDbScalingRequestModel requestModel = new CosmosDbScalingRequestModel
            {
                RepositoryTypes = new[]
               {
                    CosmosCollectionType.CalculationProviderResults
                },
                JobDefinitionId = "job-def-1"
            };

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.CalculationProviderResults);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            settings.CurrentRequestUnits = 180000;

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingConfig);
            cosmosDbScalingConfigRepository
               .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
               .Returns(settings);

            cosmosDbScalingConfigRepository
                .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
                .Returns(HttpStatusCode.OK);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();
            modelBuilder
                .BuildRequestModel(Arg.Any<JobSummary>())
                .Returns(requestModel);

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider,
                cosmosDbScalingRequestModelBuilder: modelBuilder);

            //Act
            await cosmosDbScalingService.Process(message);

            //Assert
            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Is(settings.MaxRequestUnits));
            
            await
                 cosmosDbScalingConfigRepository
                 .Received(1)
                 .UpdateCollectionSettings(Arg.Is<CosmosDbScalingCollectionSettings>(m => m.CurrentRequestUnits == settings.MaxRequestUnits));

        }

        [TestMethod]
        public async Task ScaleUp_WhenCurrentRequestUnitsAreAtMaxium_EnsuresDoesntExceedMaximum()
        {
            //Arrange
            JobSummary jobNotification = new JobSummary
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.Queued
            };

            CosmosDbScalingRequestModel requestModel = new CosmosDbScalingRequestModel
            {
                RepositoryTypes = new[]
                {
                    CosmosCollectionType.CalculationProviderResults
                },
                JobDefinitionId = "job-def-1"
            };

            string json = JsonConvert.SerializeObject(jobNotification);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.CalculationProviderResults);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            settings.CurrentRequestUnits = settings.MaxRequestUnits;

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingConfig);
            cosmosDbScalingConfigRepository
                .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
                .Returns(HttpStatusCode.OK);
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(settings);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();
            modelBuilder
                .BuildRequestModel(Arg.Any<JobSummary>())
                .Returns(requestModel);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider,
                cosmosDbScalingRequestModelBuilder: modelBuilder);

            //Act
            await cosmosDbScalingService.Process(message);

            //Assert
            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Is(settings.MaxRequestUnits));

            await
                 cosmosDbScalingConfigRepository
                 .Received(1)
                 .UpdateCollectionSettings(Arg.Is<CosmosDbScalingCollectionSettings>(m => m.CurrentRequestUnits == settings.MaxRequestUnits));
        }

        [TestMethod]
        public async Task ScaleUp_GivenEventsButNoneFilteredForProcessing_DoesNotLogFoundEvents()
        {
            //Arrange
            ICosmosDbThrottledEventsFilter cosmosDbThrottledEventsFilter = CreateCosmosDbThrottledEventsFilter();
            cosmosDbThrottledEventsFilter
                .GetUniqueCosmosDBContainerNamesFromEventData(Arg.Any<IEnumerable<EventData>>())
                .Returns(Enumerable.Empty<string>());

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(logger, cosmosDbThrottledEventsFilter: cosmosDbThrottledEventsFilter);

            //Act
            await cosmosDbScalingService.ScaleUp(Enumerable.Empty<EventData>());

            //Assert
            logger
                .DidNotReceive()
                .Information(Arg.Any<string>());
        }

        [TestMethod]
        public async Task ScaleUp_GivenEventsFilteredForProcessing_ScalesCollection()
        {
            //Arrange
            const string specsCollection = "specs";

            ICosmosDbThrottledEventsFilter cosmosDbThrottledEventsFilter = CreateCosmosDbThrottledEventsFilter();
            cosmosDbThrottledEventsFilter
                .GetUniqueCosmosDBContainerNamesFromEventData(Arg.Any<IEnumerable<EventData>>())
                .Returns(new[] { specsCollection });

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            settings.CurrentRequestUnits = 1000;

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.Specifications))
                .Returns(settings);
            cosmosDbScalingConfigRepository
               .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
               .Returns(HttpStatusCode.OK);

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.Specifications))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingRepositoryProvider,
                cosmosDbThrottledEventsFilter: cosmosDbThrottledEventsFilter,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository);

            //Act
            await cosmosDbScalingService.ScaleUp(Enumerable.Empty<EventData>());

            //Assert
            DateTime dateTime = DateTimeOffset.Now.Date;

            await
             cosmosDbScalingConfigRepository
             .Received(1)
             .UpdateCollectionSettings(Arg.Is<CosmosDbScalingCollectionSettings>(m =>
                  m.LastScalingIncrementValue == 10000 &&
                  m.LastScalingIncrementDateTime.Value.Date == dateTime &&
                  m.CurrentRequestUnits == 11000));

            await
             scalingRepository
                 .Received(1)
                 .SetThroughput(Arg.Is(11000));

            await
                 cosmosDbScalingConfigRepository
                 .Received(1)
                 .UpdateCollectionSettings(Arg.Is<CosmosDbScalingCollectionSettings>(m => m.CurrentRequestUnits == 11000));
        }

        [TestMethod]
        public async Task ScaleUp_WhenCurrentRequestUnitsNotAtBaseLine_EnsuresCorrectScalingRequestUnitIsIncremented()
        {
            //Arrange
            JobSummary jobNotification = new JobSummary
            {
                JobType = "job-def-1",
                RunningStatus = RunningStatus.Queued,
                JobId = "job-id-1"
            };

            CosmosDbScalingRequestModel requestModel = new CosmosDbScalingRequestModel
            {
                RepositoryTypes = new[]
                {
                    CosmosCollectionType.CalculationProviderResults
                },
                JobDefinitionId = "job-def-1"
            };

            string json = JsonConvert.SerializeObject(jobNotification);

            ICosmosDbScalingRequestModelBuilder modelBuilder = CreateReqestModelBuilder();
            modelBuilder
                .BuildRequestModel(Arg.Any<JobSummary>())
                .Returns(requestModel);

            Message message = new Message(Encoding.UTF8.GetBytes(json));

            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.CalculationProviderResults);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            settings.CurrentRequestUnits = 100000;

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingConfig);
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(settings);
            cosmosDbScalingConfigRepository
                .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
                .Returns(HttpStatusCode.OK);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            ILogger logger = CreateLogger();
            ITelemetry telemetry = CreateTelemetry();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider,
                cosmosDbScalingRequestModelBuilder: modelBuilder,
                telemetry: telemetry);

            //Act
            await cosmosDbScalingService.Process(message);

            //Assert
            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Is(150000));

            await
                 cosmosDbScalingConfigRepository
                 .Received(1)
                 .UpdateCollectionSettings(Arg.Is<CosmosDbScalingCollectionSettings>(m => m.CurrentRequestUnits == 150000));

            logger
                .Received(1)
                .Information("Current settings: {\"id\":\"CalculationProviderResults\",\"cosmosCollectionType\":\"CalculationProviderResults\",\"lastScalingIncrementValue\":0,\"lastScalingDecrementValue\":0,\"lastScalingIncrementDateTime\":null,\"lastScalingDeccrementDateTime\":null,\"currentRequestUnits\":100000,\"maxRequestUnits\":200000,\"minRequestUnits\":10000} has been scaled with settings: {\"id\":\"CalculationProviderResults\",\"cosmosCollectionType\":\"CalculationProviderResults\",\"lastScalingIncrementValue\":0,\"lastScalingDecrementValue\":0,\"lastScalingIncrementDateTime\":null,\"lastScalingDeccrementDateTime\":null,\"currentRequestUnits\":150000,\"maxRequestUnits\":200000,\"minRequestUnits\":10000} scaling direction: Up and type: Job");

            telemetry
                .Received(1)
                .TrackEvent("CosmosScalingCompleted", 
                Arg.Is<IDictionary<string, string>>(p => 
                                            p["collectionName"] == "calculationresults" &&
                                            p["scaleEvent"] == "Job" &&
                                            p["direction"] == "Up" &&
                                            p["jobDefinitionId"] == "job-def-1" &&
                                            p["jobId"] == "job-id-1"), 
                Arg.Is<IDictionary<string, double>>(m => 
                                            m["scaleValue"] == 150000 &&
                                            m["previousScaleValue"] == 100000 &&
                                            m["scaleDifference"] == 50000));
        }

        [TestMethod]
        public async Task ScaleUp_GivenEventsFilteredForProcessingAndIncreasingRequestUnitsWillExceedMaximum_ScalesCollectionToMaximumRequestUnits()
        {
            //Arrange
            const string specsCollection = "specs";
            const int maxRequestUnits = 200000;

            ICosmosDbThrottledEventsFilter cosmosDbThrottledEventsFilter = CreateCosmosDbThrottledEventsFilter();
            cosmosDbThrottledEventsFilter
                .GetUniqueCosmosDBContainerNamesFromEventData(Arg.Any<IEnumerable<EventData>>())
                .Returns(new[] { specsCollection });

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.Specifications);
            settings.CurrentRequestUnits = 199000;

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.Specifications))
                .Returns(settings);
            cosmosDbScalingConfigRepository
               .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
               .Returns(HttpStatusCode.OK);

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.Specifications))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingRepositoryProvider,
                cosmosDbThrottledEventsFilter: cosmosDbThrottledEventsFilter,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository);

            //Act
            await cosmosDbScalingService.ScaleUp(Enumerable.Empty<EventData>());

            //Assert
            logger
                .Received(1)
                .Information(Arg.Is($"Found 1 collections to process"));

            await
              cosmosDbScalingConfigRepository
              .Received(1)
              .UpdateCollectionSettings(Arg.Is<CosmosDbScalingCollectionSettings>(m =>
                   m.LastScalingIncrementValue == 1000 &&
                   m.LastScalingIncrementDateTime.Value.Date == DateTimeOffset.Now.Date &&
                   m.CurrentRequestUnits == maxRequestUnits
              ));

            await
                scalingRepository
                    .Received(1)
                    .SetThroughput(Arg.Is(maxRequestUnits));

            await
                 cosmosDbScalingConfigRepository
                 .Received(1)
                 .UpdateCollectionSettings(Arg.Is<CosmosDbScalingCollectionSettings>(m => m.CurrentRequestUnits == maxRequestUnits));
        }

        [TestMethod]
        public async Task ScaleUp_GivenEventsFilteredForProcessingAndCurrentRequestUnitsAlreadayAtMaximum_DoesNotScaleUp()
        {
            //Arrange
            const string specsCollection = "specs";
            const int maxRequestUnits = 200000;

            ICosmosDbThrottledEventsFilter cosmosDbThrottledEventsFilter = CreateCosmosDbThrottledEventsFilter();
            cosmosDbThrottledEventsFilter
                .GetUniqueCosmosDBContainerNamesFromEventData(Arg.Any<IEnumerable<EventData>>())
                .Returns(new[] { specsCollection });

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();
            scalingRepository
                .GetCurrentThroughput()
                .Returns(maxRequestUnits);

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.Specifications))
                .Returns(scalingRepository);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.Specifications);
            settings.CurrentRequestUnits = settings.MaxRequestUnits;

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.Specifications))
                .Returns(settings);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingRepositoryProvider,
                cosmosDbThrottledEventsFilter: cosmosDbThrottledEventsFilter,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository);

            string errorMessage = $"The collection '{specsCollection}' throughput is already at the maximum of {maxRequestUnits} RU's";

            //Act
            await cosmosDbScalingService.ScaleUp(Enumerable.Empty<EventData>());

            //Assert
            logger
                .Received(1)
                .Warning(Arg.Is(errorMessage));

            await
                scalingRepository
                    .DidNotReceive()
                    .SetThroughput(Arg.Any<int>());
        }

        [TestMethod]
        public void ScaleUp_GivenEventsFilteredForProcessingButScalingCausesException_ThrowsRetriablEexception()
        {
            //Arrange
            const string specsCollection = "specs";

            ICosmosDbThrottledEventsFilter cosmosDbThrottledEventsFilter = CreateCosmosDbThrottledEventsFilter();
            cosmosDbThrottledEventsFilter
                .GetUniqueCosmosDBContainerNamesFromEventData(Arg.Any<IEnumerable<EventData>>())
                .Returns(new[] { specsCollection });

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();
            scalingRepository
                .GetCurrentThroughput()
                .Returns(10000);

            scalingRepository
                .When(x => x.SetThroughput(Arg.Any<int>()))
                .Do(x => { throw new Exception(); });

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.Specifications))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingRepositoryProvider,
                cosmosDbThrottledEventsFilter: cosmosDbThrottledEventsFilter);

            string errorMessage = $"Failed to increase cosmosdb request units on collection '{specsCollection}'";

            //Act
            Func<Task> test = async () => await cosmosDbScalingService.ScaleUp(Enumerable.Empty<EventData>());

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be(errorMessage);

            logger
                .Received(1)
                .Error(Arg.Any<Exception>(), Arg.Is(errorMessage));
        }

        [TestMethod]
        public void ScaleDownForJobConfiguration_WhenFailingToFecthJobSummaries_ThrowsNewRetriableException()
        {
            //Arrange
            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns((IEnumerable<JobSummary>)null);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(logger, jobManagement: jobManagement);

            //Act
            Func<Task> test = async () => await cosmosDbScalingService.ScaleDownForJobConfiguration();

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
        public async Task ScaleDownForJobConfiguration_WhenJobSummariesReturnedAndConfigsReturnedFromCacheButConfigAlreadyAtBaseline_DoesNotUpdateConfig()
        {
            //Arrange
            IEnumerable<JobSummary> jobSummaries = new[]
            {
                new JobSummary
                {
                    JobType = "job-def-1"
                }
            };

            CosmosDbScalingConfig cosmosDbScalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.CalculationProviderResults);

            IEnumerable<CosmosDbScalingConfig> configs = new[] { cosmosDbScalingConfig };

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummaries);

            ILogger logger = CreateLogger();

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            settings.CurrentRequestUnits = settings.MinRequestUnits;

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(settings);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                jobManagement: jobManagement,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider);

            //Act
            await cosmosDbScalingService.ScaleDownForJobConfiguration();

            //Assert
            await
                cosmosDbScalingConfigRepository
                .DidNotReceive()
                .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>());
        }

        [TestMethod]
        public void ScaleDownForJobConfiguration_WhenJobSummariesReturnedAndConfigsReturnedFromCacheButFailsToSetThroughput_ThrowsRetriableException()
        {
            //Arrange
            CosmosDbScalingConfig cosmosDbScalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.CalculationProviderResults);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            settings.CurrentRequestUnits = settings.MaxRequestUnits;

            IEnumerable<CosmosDbScalingConfig> configs = new[] { cosmosDbScalingConfig };

            IEnumerable<JobSummary> jobSummariesResponse = Enumerable.Empty<JobSummary>();

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummariesResponse);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(settings);

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();
            scalingRepository
               .When(x => x.SetThroughput(Arg.Any<int>()))
               .Do(x => { throw new Exception(); });

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                jobManagement: jobManagement,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            Func<Task> test = async () => await cosmosDbScalingService.ScaleDownForJobConfiguration();

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
        public async Task ScaleDownForJobConfiguration_WhenJobSummariesReturnedAndConfigsReturnedFromCacheButFailsToUpdateConfig_ThrowsRetriableException()
        {
            //Arrange
            CosmosDbScalingConfig cosmosDbScalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.CalculationProviderResults);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            settings.CurrentRequestUnits = settings.MaxRequestUnits;

            IEnumerable<CosmosDbScalingConfig> configs = new[] { cosmosDbScalingConfig };

            IEnumerable<JobSummary> jobSummariesResponse = Enumerable.Empty<JobSummary>();

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummariesResponse);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(settings);

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();
            scalingRepository
                .SetThroughput(Arg.Any<int>())
                .Throws(new Exception());

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                jobManagement: jobManagement,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            Func<Task> test = async () => await cosmosDbScalingService.ScaleDownForJobConfiguration();

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
                .SetThroughput(Arg.Is(settings.MinRequestUnits));
        }

        [TestMethod]
        public async Task ScaleDownForJobConfiguration_WhenJobSummariesReturnedAndConfigsReturnedFromCacheButAlreadyAtBaseLine_DoesNotSetThroughput()
        {
            //Arrange
            IEnumerable<JobSummary> jobSummaries = new[]
            {
                new JobSummary
                {
                    JobType = "job-def-1"
                }
            };

            CosmosDbScalingConfig cosmosDbScalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.CalculationProviderResults);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            settings.CurrentRequestUnits = settings.MinRequestUnits;

            IEnumerable<CosmosDbScalingConfig> configs = new[] { cosmosDbScalingConfig };

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummaries);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(settings);

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                jobManagement: jobManagement,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            await cosmosDbScalingService.ScaleDownForJobConfiguration();

            //Assert
            await
                scalingRepository
                .DidNotReceive()
                .SetThroughput(Arg.Any<int>());
        }

        [TestMethod]
        public async Task ScaleDownForJobConfiguration_WhenJobSummariesReturnedAndConfigsReturnedFromCacheAndCurrentRequestsNotAtBaseline_SetsThroughputAndUpdatesConfig()
        {
            //Arrange
            CosmosDbScalingConfig cosmosDbScalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.CalculationProviderResults);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            settings.CurrentRequestUnits = settings.MaxRequestUnits;

            IEnumerable<CosmosDbScalingConfig> configs = new[] { cosmosDbScalingConfig };

            IEnumerable<JobSummary> jobSummariesResponse = Enumerable.Empty<JobSummary>();

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummariesResponse);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
               .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
               .Returns(HttpStatusCode.OK);

            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(settings);

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                jobManagement: jobManagement,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            await cosmosDbScalingService.ScaleDownForJobConfiguration();

            //Assert
            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Is(settings.MinRequestUnits));

            await
                cacheProvider
                .Received(1)
                .RemoveAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs));
        }

        [TestMethod]
        public async Task ScaleDownForJobConfiguration_WhenJobSummariesReturnedAndConfigsReturnedFromCacheAndCurrentRequestsButAllAtBaseline_DoesNotSetThroughputs()
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

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummaries);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            CosmosDbScalingCollectionSettings settings1 = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            settings1.CurrentRequestUnits = settings1.MinRequestUnits;
            CosmosDbScalingCollectionSettings settings2 = CreateCollectionSettings(CosmosCollectionType.ProviderSourceDatasets);
            settings2.CurrentRequestUnits = settings2.MinRequestUnits;
            CosmosDbScalingCollectionSettings settings3 = CreateCollectionSettings(CosmosCollectionType.PublishedFunding);
            settings3.CurrentRequestUnits = settings3.MinRequestUnits;

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Any<CosmosCollectionType>())
                .Returns(settings1, settings2, settings3);

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                jobManagement: jobManagement,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            await cosmosDbScalingService.ScaleDownForJobConfiguration();

            //Assert
            await
                scalingRepository
                .DidNotReceive()
                .SetThroughput(Arg.Any<int>());
        }

        [TestMethod]
        public async Task ScaleDownForJobConfiguration_WhenJobSummariesReturnedAndConfigsReturnedFromCacheAndCurrentRequestsButOnlyOneNotAtBaseline_SetsThroughputForOne()
        {
            //Arrange
            IEnumerable<CosmosDbScalingConfig> configs = CreateCosmosScalingConfigs();

            IEnumerable<JobSummary> jobSummariesResponse = Enumerable.Empty<JobSummary>();

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummariesResponse);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            CosmosDbScalingCollectionSettings settings1 = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            settings1.CurrentRequestUnits = 100000;

            CosmosDbScalingCollectionSettings settings2 = CreateCollectionSettings(CosmosCollectionType.ProviderSourceDatasets);
            settings2.CurrentRequestUnits = settings2.MinRequestUnits;

            CosmosDbScalingCollectionSettings settings3 = CreateCollectionSettings(CosmosCollectionType.PublishedFunding);
            settings3.CurrentRequestUnits = settings3.MinRequestUnits;

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
               .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
               .Returns(HttpStatusCode.OK);

            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Any<CosmosCollectionType>())
                .Returns(settings1, settings2, settings3);

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);
            ITelemetry telemetry = CreateTelemetry();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                jobManagement: jobManagement,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider,
                telemetry: telemetry);

            //Act
            await cosmosDbScalingService.ScaleDownForJobConfiguration();

            //Assert
            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Any<int>());

            logger
                .Received(1)
                .Information("Current settings: {\"id\":\"CalculationProviderResults\",\"cosmosCollectionType\":\"CalculationProviderResults\",\"lastScalingIncrementValue\":0,\"lastScalingDecrementValue\":0,\"lastScalingIncrementDateTime\":null,\"lastScalingDeccrementDateTime\":null,\"currentRequestUnits\":100000,\"maxRequestUnits\":200000,\"minRequestUnits\":10000} has been scaled with settings: {\"id\":\"CalculationProviderResults\",\"cosmosCollectionType\":\"CalculationProviderResults\",\"lastScalingIncrementValue\":0,\"lastScalingDecrementValue\":0,\"lastScalingIncrementDateTime\":null,\"lastScalingDeccrementDateTime\":null,\"currentRequestUnits\":10000,\"maxRequestUnits\":200000,\"minRequestUnits\":10000} scaling direction: Down and type: Job");

            telemetry
              .Received(1)
              .TrackEvent("CosmosScalingCompleted",
              Arg.Is<IDictionary<string, string>>(p =>
                                          p["collectionName"] == "calculationresults" &&
                                          p["scaleEvent"] == "Job" &&
                                          p["direction"] == "Down" &&
                                          p["jobDefinitionId"] == "NA" &&
                                          p["jobId"] == "NA"),
              Arg.Is<IDictionary<string, double>>(m =>
                                          m["scaleValue"] == 10000 &&
                                          m["previousScaleValue"] == 100000 &&
                                          m["scaleDifference"] == -90000));
        }

        [TestMethod]
        public async Task ScaleDownForJobConfiguration_WhenJobSummariesReturnedAndConfigsReturnedFromCacheAndCurrentRequestsButOnlyOneNotAtBaselineButExceedsMinimumThroughput_SetsMinimumThroughputForOne()
        {
            //Arrange
            IEnumerable<CosmosDbScalingConfig> configs = CreateCosmosScalingConfigs();

            IEnumerable<JobSummary> jobSummariesResponse = Enumerable.Empty<JobSummary>();

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummariesResponse);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            CosmosDbScalingCollectionSettings settings1 = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            settings1.CurrentRequestUnits = 100000;

            CosmosDbScalingCollectionSettings settings2 = CreateCollectionSettings(CosmosCollectionType.ProviderSourceDatasets);
            settings2.CurrentRequestUnits = settings2.MinRequestUnits;

            CosmosDbScalingCollectionSettings settings3 = CreateCollectionSettings(CosmosCollectionType.PublishedFunding);
            settings3.CurrentRequestUnits = settings3.MinRequestUnits;

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
               .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
               .Returns(HttpStatusCode.OK);

            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Any<CosmosCollectionType>())
                .Returns(settings1, settings2, settings3);

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            scalingRepository.GetMinimumThroughput()
                .Returns(35000);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                jobManagement: jobManagement,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            await cosmosDbScalingService.ScaleDownForJobConfiguration();

            //Assert
            await
                scalingRepository
                .Received(1)
                .SetThroughput(35000);
        }


        [TestMethod]
        public async Task ScaleDownForJobConfiguration_WhenActiveJobSummariesReturnedAndConfigsReturnedFromCacheAndCurrentRequestsButOnlyOneNotAtBaseline_SetsThroughputForOne()
        {
            //Arrange
            IEnumerable<CosmosDbScalingConfig> configs = new[]
            {
                new CosmosDbScalingConfig
                {
                    RepositoryType = CosmosCollectionType.CalculationProviderResults,
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
                    RepositoryType = CosmosCollectionType.ProviderSourceDatasets,
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
                    RepositoryType = CosmosCollectionType.PublishedFunding,
                    Id = "1",
                    JobRequestUnitConfigs = new[]
                    {
                        new CosmosDbScalingJobConfig
                        {
                            JobDefinitionId = "job-def-3",
                            JobRequestUnits = 20000
                        }
                    }
                },
                new CosmosDbScalingConfig
                {
                    RepositoryType = CosmosCollectionType.Specifications,
                    Id = "1",
                    JobRequestUnitConfigs = new[]
                    {
                        new CosmosDbScalingJobConfig
                        {
                            JobDefinitionId = "job-def-4",
                            JobRequestUnits = 20000
                        }
                    }
                }
            };

            IEnumerable<JobSummary> jobSummariesResponse = Enumerable.Empty<JobSummary>();

            IJobManagement jobManagement = CreateJobManagement();
            jobManagement
                .GetNonCompletedJobsWithinTimeFrame(Arg.Any<DateTimeOffset>(), Arg.Any<DateTimeOffset>())
                .Returns(jobSummariesResponse);

            ICacheProvider cacheProvider = CreateCacheProvider();
            cacheProvider
                .GetAsync<List<CosmosDbScalingConfig>>(Arg.Is(CacheKeys.AllCosmosScalingConfigs))
                .Returns(configs.ToList());

            CosmosDbScalingCollectionSettings settings1 = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            settings1.CurrentRequestUnits = 100000;

            CosmosDbScalingCollectionSettings settings2 = CreateCollectionSettings(CosmosCollectionType.ProviderSourceDatasets);
            settings2.CurrentRequestUnits = settings2.MinRequestUnits;

            CosmosDbScalingCollectionSettings settings3 = CreateCollectionSettings(CosmosCollectionType.PublishedFunding);
            settings3.CurrentRequestUnits = settings3.MinRequestUnits;

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
               .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
               .Returns(HttpStatusCode.OK);

            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Any<CosmosCollectionType>())
                .Returns(settings1, settings2, settings3);

            ILogger logger = CreateLogger();

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                jobManagement: jobManagement,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cacheProvider: cacheProvider,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            await cosmosDbScalingService.ScaleDownForJobConfiguration();

            //Assert           
            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Any<int>());

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset hourAgo = now.AddHours(-1);

            await
                jobManagement
                .Received(1)
                .GetNonCompletedJobsWithinTimeFrame(Arg.Is<DateTimeOffset>(x => x.AddHours(-1) < DateTimeOffset.Now), Arg.Any<DateTimeOffset>());

            logger
                .Received(1)
                .Information("Current settings: {\"id\":\"CalculationProviderResults\",\"cosmosCollectionType\":\"CalculationProviderResults\",\"lastScalingIncrementValue\":0,\"lastScalingDecrementValue\":0,\"lastScalingIncrementDateTime\":null,\"lastScalingDeccrementDateTime\":null,\"currentRequestUnits\":100000,\"maxRequestUnits\":200000,\"minRequestUnits\":10000} has been scaled with settings: {\"id\":\"CalculationProviderResults\",\"cosmosCollectionType\":\"CalculationProviderResults\",\"lastScalingIncrementValue\":0,\"lastScalingDecrementValue\":0,\"lastScalingIncrementDateTime\":null,\"lastScalingDeccrementDateTime\":null,\"currentRequestUnits\":10000,\"maxRequestUnits\":200000,\"minRequestUnits\":10000} scaling direction: Down and type: Job");
        }

        [TestMethod]
        public async Task ScaleDownIncrementally_GivenNoCollectionsToProcess_Exists()
        {
            //Arrange
            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsIncremented(Arg.Any<int>())
                .Returns(Enumerable.Empty<CosmosDbScalingCollectionSettings>());

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(logger, cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository);

            //Act
            await cosmosDbScalingService.ScaleDownIncrementally();

            //Assert
            logger
                .DidNotReceive()
                .Information(Arg.Any<string>());
        }

        [TestMethod]
        public async Task ScaleDownIncrementally_GivenOneCollectionsToProcess_ScalesCollection()
        {
            //Arrange
            CosmosDbScalingCollectionSettings cosmosDbScalingCollectionSettings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            cosmosDbScalingCollectionSettings.CurrentRequestUnits = 20000;
            cosmosDbScalingCollectionSettings.LastScalingIncrementValue = 10000;

            IEnumerable<CosmosDbScalingCollectionSettings> collectionsToProcess = new[]
            {
                cosmosDbScalingCollectionSettings
            };

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsIncremented(Arg.Any<int>())
                .Returns(collectionsToProcess);
            cosmosDbScalingConfigRepository
                .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
                .Returns(HttpStatusCode.OK);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            await cosmosDbScalingService.ScaleDownIncrementally();

            //Assert
            logger
                .Received()
                .Information(Arg.Is($"Found {collectionsToProcess.Count()} collections to scale down"));

            await
              cosmosDbScalingConfigRepository
              .Received(1)
              .UpdateCollectionSettings(Arg.Is<CosmosDbScalingCollectionSettings>(m =>
                   m.CurrentRequestUnits == 10000 &&
                   m.LastScalingIncrementDateTime == null &&
                   m.LastScalingIncrementValue == 0 &&
                   m.LastScalingDecrementValue == 10000 &&
                   m.LastScalingDecrementDateTime.HasValue
              ));

            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Is(10000));
        }

        [TestMethod]
        public async Task ScaleDownIncrementally_GivenOneCollectionsToProcessAndLastIncremnetWas10000_ScalesCollectionEnsuresCurrentUpdated()
        {
            //Arrange
            CosmosDbScalingCollectionSettings cosmosDbScalingCollectionSettings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            cosmosDbScalingCollectionSettings.CurrentRequestUnits = 50000;
            cosmosDbScalingCollectionSettings.LastScalingIncrementValue = 10000;

            IEnumerable<CosmosDbScalingCollectionSettings> collectionsToProcess = new[]
            {
                cosmosDbScalingCollectionSettings
            };

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsIncremented(Arg.Any<int>())
                .Returns(collectionsToProcess);
            cosmosDbScalingConfigRepository
                .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
                .Returns(HttpStatusCode.OK);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            ILogger logger = CreateLogger();
            ITelemetry telemetry = CreateTelemetry();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider,
                telemetry: telemetry);

            //Act
            await cosmosDbScalingService.ScaleDownIncrementally();

            //Assert
            logger
                .Received()
                .Information(Arg.Is($"Found {collectionsToProcess.Count()} collections to scale down"));

            await
              cosmosDbScalingConfigRepository
              .Received(1)
              .UpdateCollectionSettings(Arg.Is<CosmosDbScalingCollectionSettings>(m =>
                   m.CurrentRequestUnits == 40000 &&
                   m.LastScalingIncrementDateTime == null &&
                   m.LastScalingIncrementValue == 0 &&
                   m.LastScalingDecrementValue == 10000 &&
                   m.LastScalingDecrementDateTime.HasValue
              ));

            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Is(40000));

            telemetry
               .Received(1)
               .TrackEvent("CosmosScalingCompleted",
               Arg.Is<IDictionary<string, string>>(p =>
                                           p["collectionName"] == "calculationresults" &&
                                           p["scaleEvent"] == "Incremental" &&
                                           p["direction"] == "Down" &&
                                           p["jobDefinitionId"] == "NA" &&
                                           p["jobId"] == "NA"),
               Arg.Is<IDictionary<string, double>>(m =>
                                           m["scaleValue"] == 40000 &&
                                           m["previousScaleValue"] == 50000 &&
                                           m["scaleDifference"] == -10000));
        }

        [TestMethod]
        public async Task ScaleDownIncrementally_GivenOneCollectionsToProcessAndLastIncremnetWas20000_ScalesCollectionEnsuresCurrentUpdatedWithMinimumThroughput()
        {
            //Arrange
            CosmosDbScalingCollectionSettings cosmosDbScalingCollectionSettings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            cosmosDbScalingCollectionSettings.CurrentRequestUnits = 50000;
            cosmosDbScalingCollectionSettings.LastScalingIncrementValue = 20000;

            IEnumerable<CosmosDbScalingCollectionSettings> collectionsToProcess = new[]
            {
                cosmosDbScalingCollectionSettings
            };

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsIncremented(Arg.Any<int>())
                .Returns(collectionsToProcess);
            cosmosDbScalingConfigRepository
                .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
                .Returns(HttpStatusCode.OK);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);
            scalingRepository.GetMinimumThroughput()
                .Returns(35000);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            await cosmosDbScalingService.ScaleDownIncrementally();

            //Assert
            logger
                .Received()
                .Information(Arg.Is($"Found {collectionsToProcess.Count()} collections to scale down"));

            await
              cosmosDbScalingConfigRepository
              .Received(1)
              .UpdateCollectionSettings(Arg.Is<CosmosDbScalingCollectionSettings>(m =>
                   m.CurrentRequestUnits == 35000 &&
                   m.LastScalingIncrementDateTime == null &&
                   m.LastScalingIncrementValue == 0 &&
                   m.LastScalingDecrementValue == 15000 &&
                   m.LastScalingDecrementDateTime.HasValue
              ));

            await
                scalingRepository
                .Received(1)
                .SetThroughput(Arg.Is(35000));
        }

        [TestMethod]
        public async Task ScaleDownIncrementally_GivenOneCollectionsToProcessAndLastIncremnetWas10000AndCurrentAt10000_DoesnOtScalecollection()
        {
            //Arrange
            CosmosDbScalingCollectionSettings cosmosDbScalingCollectionSettings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            cosmosDbScalingCollectionSettings.CurrentRequestUnits = 10000;
            cosmosDbScalingCollectionSettings.LastScalingIncrementValue = 10000;

            IEnumerable<CosmosDbScalingCollectionSettings> collectionsToProcess = new[]
            {
                cosmosDbScalingCollectionSettings
            };

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsIncremented(Arg.Any<int>())
                .Returns(collectionsToProcess);
            cosmosDbScalingConfigRepository
                .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
                .Returns(HttpStatusCode.OK);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            await cosmosDbScalingService.ScaleDownIncrementally();

            //Assert
            logger
                .Received()
                .Information(Arg.Is($"Found {collectionsToProcess.Count()} collections to scale down"));

            await
                scalingRepository
                .DidNotReceive()
                .SetThroughput(Arg.Any<int>());
        }

        [TestMethod]
        public void ScaleDownIncrementally_GivenOneCollectionsToProcessButFailsToScale_ThrowsretriableException()
        {
            //Arrange
            CosmosDbScalingCollectionSettings cosmosDbScalingCollectionSettings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);
            cosmosDbScalingCollectionSettings.CurrentRequestUnits = 20000;
            cosmosDbScalingCollectionSettings.LastScalingIncrementValue = 10000;

            IEnumerable<CosmosDbScalingCollectionSettings> collectionsToProcess = new[]
            {
                cosmosDbScalingCollectionSettings
            };

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            cosmosDbScalingConfigRepository
                .GetCollectionSettingsIncremented(Arg.Any<int>())
                .Returns(collectionsToProcess);
            cosmosDbScalingConfigRepository
                .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
                .Returns(HttpStatusCode.BadRequest);

            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingRepository);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            Func<Task> test = async () => await cosmosDbScalingService.ScaleDownIncrementally();

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Failed to scale down collection for repository type '{CosmosCollectionType.CalculationProviderResults}'");

            logger
                .Received(1)
                .Error(Arg.Is($"Failed to update cosmos scale config repository type: '{CosmosCollectionType.CalculationProviderResults}' with new request units of '10000' with status code: 'BadRequest'"));
        }

        [TestMethod]
        public async Task SaveConfiguration_GivenModelButWasInvalid_ReturnesBadRequest()
        {
            //Arrange
            ScalingConfigurationUpdateModel request = CreateScalingConfigurationUpdateModel();

            ILogger logger = CreateLogger();

            ValidationResult validationResult = new ValidationResult(new[]{
                    new ValidationFailure("prop1", "any error")
                });

            IValidator<ScalingConfigurationUpdateModel> validator = CreateScalingConfigurationUpdateModelValidator(validationResult);


            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                scalingConfigurationUpdateModelValidator: validator);

            //Act
            IActionResult result = await cosmosDbScalingService.SaveConfiguration(request);

            //Assert
            result
                .Should()
                .BeOfType<BadRequestObjectResult>()
                .Which
                .Value
                .Should()
                .NotBeNull();

            BadRequestObjectResult badRequestObjectResult = result as BadRequestObjectResult;
            badRequestObjectResult
                .Value
                .Should()
                .BeOfType<SerializableError>();

            SerializableError modelState = badRequestObjectResult.Value as SerializableError;
            modelState
                .Should()
                .NotBeNull();

            modelState
                .Values
                .Should()
                .HaveCount(1);

            modelState
                .First()
                .Key
                .Should()
                .Be("prop1");

            modelState
                .First()
                .Value
                .Should()
                .BeEquivalentTo(new[] { "any error" });
        }

        [TestMethod]
        public void SaveConfiguration_GivenScalingCollectionsToProcessButFailsToUpdate_ThrowsretriableException()
        {
            //Arrange
            ScalingConfigurationUpdateModel request = CreateScalingConfigurationUpdateModel();

            IValidator<ScalingConfigurationUpdateModel> validator = CreateScalingConfigurationUpdateModelValidator(null);

            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.CalculationProviderResults);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();

            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingConfig);

            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(settings);

            cosmosDbScalingConfigRepository
                .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
                .Returns(HttpStatusCode.InternalServerError);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                 scalingConfigurationUpdateModelValidator: validator);


            //Act
            Func<Task> test = async () => await cosmosDbScalingService.SaveConfiguration(request);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Failed to Insert or Update Scaling Collection Setting for repository type: '{request.RepositoryType}'  with status code: '{HttpStatusCode.InternalServerError}'");


        }

        [TestMethod]
        public void SaveConfiguration_GivenScalingConfigToProcessButFailsToUpdate_ThrowsretriableException()
        {
            //Arrange
            ScalingConfigurationUpdateModel request = CreateScalingConfigurationUpdateModel();

            IValidator<ScalingConfigurationUpdateModel> validator = CreateScalingConfigurationUpdateModelValidator(null);


            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.CalculationProviderResults);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();

            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingConfig);

            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(settings);

            cosmosDbScalingConfigRepository
                .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
                .Returns(HttpStatusCode.OK);

            cosmosDbScalingConfigRepository
                .UpdateConfigSettings(Arg.Any<CosmosDbScalingConfig>())
                .Returns(HttpStatusCode.InternalServerError);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                 scalingConfigurationUpdateModelValidator: validator);


            //Act
            Func<Task> test = async () => await cosmosDbScalingService.SaveConfiguration(request);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Failed to Insert or Update config setting repository type: '{request.RepositoryType}'  with status code: '{HttpStatusCode.InternalServerError}'");
        }

        [TestMethod]
        public async Task SaveConfiguration_GivenScalingCollectionsToProcessNoExistingDocument_InsertScaleCollectionAsync()
        {
            //Arrange
            ScalingConfigurationUpdateModel request = CreateScalingConfigurationUpdateModel();

            IValidator<ScalingConfigurationUpdateModel> validator = CreateScalingConfigurationUpdateModelValidator(null);


            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.CalculationProviderResults);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.CalculationProviderResults);

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();

            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(scalingConfig);

            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.CalculationProviderResults))
                .Returns(new CosmosDbScalingCollectionSettings());

            cosmosDbScalingConfigRepository
                .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
                .Returns(HttpStatusCode.OK);

            cosmosDbScalingConfigRepository
                .UpdateConfigSettings(Arg.Any<CosmosDbScalingConfig>())
                .Returns(HttpStatusCode.OK);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                 scalingConfigurationUpdateModelValidator: validator);


            //Act
            await cosmosDbScalingService.SaveConfiguration(request);

            //Assert
            await
              cosmosDbScalingConfigRepository
              .Received(1)
              .UpdateCollectionSettings(Arg.Is<CosmosDbScalingCollectionSettings>(m =>
                   m.CosmosCollectionType == request.RepositoryType &&
                   m.MaxRequestUnits == request.MaxRequestUnits &&
                   m.MinRequestUnits == request.BaseRequestUnits
              ));

        }

        [TestMethod]
        public async Task SaveConfiguration_GivenScalingCollectionsToProcessExistingDocument_UpdateScaleCollectionAsync()
        {
            //Arrange
            ScalingConfigurationUpdateModel request = CreateScalingConfigurationUpdateModel();

            IValidator<ScalingConfigurationUpdateModel> validator = CreateScalingConfigurationUpdateModelValidator(null);


            CosmosDbScalingConfig scalingConfig = CreateCosmosScalingConfig(CosmosCollectionType.ProviderSourceDatasets);

            CosmosDbScalingCollectionSettings settings = CreateCollectionSettings(CosmosCollectionType.ProviderSourceDatasets);

            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();

            cosmosDbScalingConfigRepository
                .GetConfigByRepositoryType(Arg.Is(CosmosCollectionType.ProviderSourceDatasets))
                .Returns(scalingConfig);

            cosmosDbScalingConfigRepository
                .GetCollectionSettingsByRepositoryType(Arg.Is(CosmosCollectionType.ProviderSourceDatasets))
                .Returns(settings);

            cosmosDbScalingConfigRepository
                .UpdateCollectionSettings(Arg.Any<CosmosDbScalingCollectionSettings>())
                .Returns(HttpStatusCode.OK);

            cosmosDbScalingConfigRepository
                .UpdateConfigSettings(Arg.Any<CosmosDbScalingConfig>())
                .Returns(HttpStatusCode.OK);

            ILogger logger = CreateLogger();

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                logger,
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                 scalingConfigurationUpdateModelValidator: validator);


            //Act
            await cosmosDbScalingService.SaveConfiguration(request);

            //Assert
            await
              cosmosDbScalingConfigRepository
              .Received(1)
              .UpdateCollectionSettings(Arg.Is<CosmosDbScalingCollectionSettings>(m =>
                   m.CosmosCollectionType == request.RepositoryType &&
                   m.MaxRequestUnits == request.MaxRequestUnits &&
                   m.MinRequestUnits == request.BaseRequestUnits
              ));

            await cosmosDbScalingConfigRepository
             .Received(1)
             .UpdateConfigSettings(Arg.Is<CosmosDbScalingConfig>(m =>
                  m.JobRequestUnitConfigs == request.JobRequestUnitConfigs
             ));
        }

        [TestMethod]
        [DataRow(1000, 1001, 1000)]
        [DataRow(1001, 1000, 1000)]
        [DataRow(1000, 1000, 1000)]
        public async Task ScaleCollection_CallsScalingRepositoryCorrectly(int requestedRUs,
            int maxAllowedRUs,
            int expectedRUs)
        {
            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = CreateCosmosDbScalingConfigRepository();
            ICosmosDbScalingRepository scalingRepository = CreateCosmosDbScalingRepository();

            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = CreateCosmosDbScalingRepositoryProvider();
            cosmosDbScalingRepositoryProvider
                .GetRepository(Arg.Is(CosmosCollectionType.ProviderSourceDatasets))
                .Returns(scalingRepository);

            CosmosDbScalingService cosmosDbScalingService = CreateScalingService(
                cosmosDbScalingConfigRepository: cosmosDbScalingConfigRepository,
                cosmosDbScalingRepositoryProvider: cosmosDbScalingRepositoryProvider);

            //Act
            await cosmosDbScalingService.ScaleCollection(CosmosCollectionType.ProviderSourceDatasets, requestedRUs, maxAllowedRUs);

            //Assert
            await
                scalingRepository
                .Received(1)
                .SetThroughput(expectedRUs);
        }

        private static ScalingConfigurationUpdateModel CreateScalingConfigurationUpdateModel()
        {
            return new ScalingConfigurationUpdateModel()
            {
                BaseRequestUnits = 4000,
                JobRequestUnitConfigs = new[]
                {
                    new CosmosDbScalingJobConfig
                    {
                        JobDefinitionId = "job-def-1",
                        JobRequestUnits = 50000
                    }
                },
                MaxRequestUnits = 4000,
                RepositoryType = CosmosCollectionType.ProviderSourceDatasets,
            };
        }

        private CosmosDbScalingService CreateScalingService(
            ILogger logger = null,
            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider = null,
            IJobManagement jobManagement = null,
            ICacheProvider cacheProvider = null,
            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository = null,
            ICosmosDbScalingRequestModelBuilder cosmosDbScalingRequestModelBuilder = null,
            ICosmosDbThrottledEventsFilter cosmosDbThrottledEventsFilter = null,
            IValidator<ScalingConfigurationUpdateModel> scalingConfigurationUpdateModelValidator = null,
            ITelemetry telemetry = null)
        {
            return new CosmosDbScalingService(
                logger ?? CreateLogger(),
                cosmosDbScalingRepositoryProvider ?? CreateCosmosDbScalingRepositoryProvider(),
                jobManagement ?? CreateJobManagement(),
                cacheProvider ?? CreateCacheProvider(),
                cosmosDbScalingConfigRepository ?? CreateCosmosDbScalingConfigRepository(),
                CosmosDbScalingResilienceTestHelper.GenerateTestPolicies(),
                cosmosDbScalingRequestModelBuilder ?? CreateReqestModelBuilder(),
                cosmosDbThrottledEventsFilter ?? CreateCosmosDbThrottledEventsFilter(),
                scalingConfigurationUpdateModelValidator ?? CreateScalingConfigurationUpdateModelValidator(),
                telemetry ?? CreateTelemetry()
                );
            
        }

        private ITelemetry CreateTelemetry()
        {
            return Substitute.For<ITelemetry>();
        }

        static IValidator<ScalingConfigurationUpdateModel> CreateScalingConfigurationUpdateModelValidator(ValidationResult validationResult = null)
        {
            if (validationResult == null)
            {
                validationResult = new ValidationResult();
            }

            IValidator<ScalingConfigurationUpdateModel> validator = Substitute.For<IValidator<ScalingConfigurationUpdateModel>>();

            validator
               .ValidateAsync(Arg.Any<ScalingConfigurationUpdateModel>())
               .Returns(validationResult);

            return validator;
        }



        private static ICosmosDbThrottledEventsFilter CreateCosmosDbThrottledEventsFilter()
        {
            return Substitute.For<ICosmosDbThrottledEventsFilter>();
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

        private static IJobManagement CreateJobManagement()
        {
            return Substitute.For<IJobManagement>();
        }

        private static ICosmosDbScalingRepository CreateCosmosDbScalingRepository()
        {
            return Substitute.For<ICosmosDbScalingRepository>();
        }

        private static CosmosDbScalingConfig CreateCosmosScalingConfig(CosmosCollectionType cosmosRepositoryType)
        {
            return new CosmosDbScalingConfig
            {
                RepositoryType = cosmosRepositoryType,

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

        private static CosmosDbScalingCollectionSettings CreateCollectionSettings(CosmosCollectionType collectionType)
        {
            return new CosmosDbScalingCollectionSettings
            {
                CosmosCollectionType = collectionType,
                MinRequestUnits = 10000,
                MaxRequestUnits = 200000,
                CurrentRequestUnits = 10000,
            };
        }

        private static IEnumerable<CosmosDbScalingConfig> CreateCosmosScalingConfigs()
        {
            return new[]
            {
                new CosmosDbScalingConfig
                {
                    RepositoryType = CosmosCollectionType.CalculationProviderResults,
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
                    RepositoryType = CosmosCollectionType.ProviderSourceDatasets,
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
                    RepositoryType = CosmosCollectionType.PublishedFunding,
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
