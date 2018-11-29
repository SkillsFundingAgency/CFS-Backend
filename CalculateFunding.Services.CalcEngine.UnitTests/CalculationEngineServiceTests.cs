﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Calculator.Interfaces;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;

namespace CalculateFunding.Services.Calculator
{
    [TestClass]
    public class CalculationEngineServiceTests
    {
        [TestMethod]
        public void GenerateAllocations_WhenBuildProjectIsNull_ShouldThrowException()
        {
            // Arrange
            const string cacheKey = "Cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;

            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-id", specificationId);

            // Act
            Action serviceAction = () => { service.GenerateAllocations(message).Wait(); };

            // Assert
            serviceAction.ShouldThrow<ArgumentNullException>();
        }

        [Ignore("This test has a provider result as null, but should be checking successful results.")]
        [TestMethod]
        public async Task GenerateAllocations_GivenAValidRequestWhereSaveProviderResultsNotIgnored_ShouldBatchCorrectlyAndSaveProviderResults()
        {
            const string cacheKey = "Cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;

            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            BuildProject buildProject = CreateBuildProject();

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new List<CalculationResult>());

            calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);
            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>())
                .Returns((ProviderResult)null);

            calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-id", specificationId);

            await service.GenerateAllocations(message);

            await calculationEngineServiceTestsHelper
                .MockProviderResultRepo
                .Received(0)
                .SaveProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Any<int>());
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenAValidRequestWhereNoResultsWereReturned_ShouldNotSaveAnything()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;

            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            BuildProject buildProject = CreateBuildProject();

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new List<CalculationResult>());

            calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);
            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>())
                .Returns(new ProviderResult()
                {

                });

            calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-id", specificationId);


            //Act
            await service.GenerateAllocations(message);

            //Assert
            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>());

            await
            calculationEngineServiceTestsHelper
                .MockProviderResultRepo
                .Received(7)
                .SaveProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Any<int>());
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenAValidRequestWhereNoResultsWereReturnedAndFeatureToggleIsEnabled_CallsSaveSevenTimes()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;

            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            BuildProject buildProject = CreateBuildProject();

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new List<CalculationResult>());

            calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            IEnumerable<DatasetAggregations> datasetAggregations = new[]
            {
                 new DatasetAggregations(),
                 new DatasetAggregations()
            };

            calculationEngineServiceTestsHelper
                .DatasetAggregationsRepository
                .GetDatasetAggregationsForSpecificationId(Arg.Is(specificationId))
                .Returns(datasetAggregations);

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);

            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Is(datasetAggregations))
                .Returns(new ProviderResult()
                {});

            calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            calculationEngineServiceTestsHelper
                .FeatureToggle
                .IsAggregateSupportInCalculationsEnabled()
                .Returns(true);


            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-id", specificationId);


            //Act
            await service.GenerateAllocations(message);

            //Assert
            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Is(datasetAggregations));

            await
            calculationEngineServiceTestsHelper
                .MockProviderResultRepo
                .Received(6)
                .SaveProviderResults(Arg.Is<IEnumerable<ProviderResult>>(m => m.Count() == 3), Arg.Any<int>());

            await
            calculationEngineServiceTestsHelper
                .MockProviderResultRepo
                .Received(1)
                .SaveProviderResults(Arg.Is<IEnumerable<ProviderResult>>(m => m.Count() == 2), Arg.Any<int>());
        }

        [TestMethod]
        public async Task
            GenerateAllocations_GivenAValidRequestWhereIgnoreSaveProviderResultsFlagIsSet_ShouldNotSaveProviderResults()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;

            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            BuildProject buildProject = CreateBuildProject();

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new List<CalculationResult>());

            calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);
            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>())
                .Returns(new ProviderResult()
                {

                });

            calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("ignore-save-provider-results", "true");

            //Act
            await service.GenerateAllocations(message);

            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Is((IEnumerable<DatasetAggregations>)null));

            //Assert
            await
                calculationEngineServiceTestsHelper
                    .MockProviderResultRepo
                    .Received(0)
                    .SaveProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Any<int>());
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenIsJobServiceEnabledSwitcheOnButJobIdMisingFromMessage_LogsErrorDoesNotAddJoblog()
        {
            //Arrange
            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            Message message = new Message();

            calculationEngineServiceTestsHelper
                .FeatureToggle
                .IsJobServiceEnabled()
                .Returns(true);

            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            //Act
            await service.GenerateAllocations(message);

            //Assert
            await
                calculationEngineServiceTestsHelper
                    .MockJobsRepository
                    .DidNotReceive()
                    .AddJobLog(Arg.Any<string>(), Arg.Any<JobLogUpdateModel>());

            calculationEngineServiceTestsHelper
                .MockLogger
                .Received(1)
                .Error("Missing job id for generating allocations");
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenIsJobServiceEnabledSwitcheOn_EnsuresJobLogsAdded()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;
            const string jobId = "job-id-1";

            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            BuildProject buildProject = CreateBuildProject();

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new List<CalculationResult>());

            calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            calculationEngineServiceTestsHelper
                .FeatureToggle
                .IsJobServiceEnabled()
                .Returns(true);

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);
            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>())
                .Returns(new ProviderResult()
                {

                });

            calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("ignore-save-provider-results", "true");
            messageUserProperties.Add("jobId", jobId);

            //Act
            await service.GenerateAllocations(message);

            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Is((IEnumerable<DatasetAggregations>)null));

            //Assert
            await
                calculationEngineServiceTestsHelper
                    .MockJobsRepository
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m => m.CompletedSuccessfully == null));

            await
                calculationEngineServiceTestsHelper
                    .MockJobsRepository
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(
                        m => m.CompletedSuccessfully.Value &&
                             m.ItemsSucceeded == 20 &&
                             m.ItemsFailed == 0 &&
                             m.ItemsProcessed == 20 &&
                             m.Outcome == "20 provider results were generated successfully from 20 providers"));
        }

        [TestMethod]
        public async Task UpdateDeadLetteredJobLog_GivenMessageButNoJobId_LogsAnErrorAndDoesNotUpdadeJobLog()
        {
            //Arrange
            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            calculationEngineServiceTestsHelper
               .FeatureToggle
               .IsJobServiceEnabled()
               .Returns(true);

            Message message = new Message();

            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            //Act
            await service.UpdateDeadLetteredJobLog(message);

            //Assert
            calculationEngineServiceTestsHelper
                .MockLogger
                .Received(1)
                .Error(Arg.Is("Missing job id from dead lettered message"));

            await
                calculationEngineServiceTestsHelper
                    .MockJobsRepository
                    .DidNotReceive()
                    .AddJobLog(Arg.Any<string>(), Arg.Any<JobLogUpdateModel>());
        }

        [TestMethod]
        public async Task UpdateDeadLetteredJobLog_GivenMessageButAddingLogCausesException_LogsAnError()
        {
            //Arrange
            const string jobId = "job-id-1";

            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            calculationEngineServiceTestsHelper
                .FeatureToggle
                .IsJobServiceEnabled()
                .Returns(true);
;
            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            calculationEngineServiceTestsHelper
                    .MockJobsRepository
                    .When(x => x.AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>()))
                    .Do(x => { throw new Exception(); });

            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            //Act
            await service.UpdateDeadLetteredJobLog(message);

            //Assert
            calculationEngineServiceTestsHelper
                .MockLogger
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

            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            calculationEngineServiceTestsHelper
               .FeatureToggle
               .IsJobServiceEnabled()
               .Returns(true);

            Message message = new Message();
            message.UserProperties.Add("jobId", jobId);

            calculationEngineServiceTestsHelper
                    .MockJobsRepository
                    .AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>())
                    .Returns(jobLog);

            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            //Act
            await service.UpdateDeadLetteredJobLog(message);

            //Assert
            calculationEngineServiceTestsHelper
                .MockLogger
                .Received(1)
                .Information(Arg.Is($"A new job log was added to inform of a dead lettered message with job log id '{jobLog.Id}' on job with id '{jobId}' while attempting to generate allocations"));
        }

        [TestMethod]
        public async Task UpdateDeadLetteredJobLog_GivenMessageAndFeatureToggleIsTurnedOff_DoesNotAddJobLog()
        {
            //Arrange
            JobLog jobLog = new JobLog
            {
                Id = "job-log-id-1"
            };

            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            calculationEngineServiceTestsHelper
               .FeatureToggle
               .IsJobServiceEnabled()
               .Returns(false);

            Message message = new Message();

            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            //Act
            await service.UpdateDeadLetteredJobLog(message);

            //Assert
            await
                calculationEngineServiceTestsHelper
                    .MockJobsRepository
                    .DidNotReceive()
                    .AddJobLog(Arg.Any<string>(), Arg.Any<JobLogUpdateModel>());
        }

        private static BuildProject CreateBuildProject()
        {
            BuildProject buildProject = JsonConvert.DeserializeObject<BuildProject>(MockData.SerializedBuildProject());

            return buildProject;
        }

        private static IList<CalculationSummaryModel> CreateDummyCalculationSummaryModels()
        {
            List<CalculationSummaryModel> calculationSummaryModels = new List<CalculationSummaryModel>()
            {
                new CalculationSummaryModel()
                {
                    Name = "TestCalc1",
                    CalculationType = CalculationType.Funding,
                    Id = "TC1"
                },
                new CalculationSummaryModel()
                {
                    Name = "TestCalc2",
                    CalculationType = CalculationType.Number,
                    Id = "TC2"
                }
            };
            return calculationSummaryModels;
        }
    }
}
