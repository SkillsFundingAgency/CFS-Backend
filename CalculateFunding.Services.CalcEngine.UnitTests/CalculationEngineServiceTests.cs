using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
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
            const string jobId = "job1";

            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            ApiResponse<JobViewModel> jobViewModel = new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId });

            calculationEngineServiceTestsHelper
                .MockJobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModel);

            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("jobId", jobId);

            // Act
            Action serviceAction = () => { service.GenerateAllocations(message).Wait(); };

            // Assert
            serviceAction.Should().ThrowExactly<ArgumentNullException>();
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
            const string jobId = "jobId";

            Assembly assembly = typeof(CalculationEngineServiceTestsHelper).Assembly;

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
               .MockCalculationRepository
               .GetAssemblyBySpecificationId(Arg.Is(specificationId))
               .Returns(MockData.GetMockAssembly());

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
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
                .Returns(new ProviderResult()
                {

                });

            calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            ApiResponse<JobViewModel> jobViewModel = new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId });

            calculationEngineServiceTestsHelper
                .MockJobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModel);

            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("jobId", jobId);

            //Act
            await service.GenerateAllocations(message);

            //Assert
            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<CalculationAggregation>>());

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
            const string jobId = "job1";

            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            BuildProject buildProject = CreateBuildProject();

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            Dictionary<string, List<decimal>> cachedCalculationAggregates = new Dictionary<string, List<decimal>>();

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new List<CalculationResult>());

            calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            calculationEngineServiceTestsHelper
              .MockCacheProvider
              .GetAsync<Dictionary<string, List<decimal>>>($"{CacheKeys.CalculationAggregations}{specificationId}")
              .Returns(cachedCalculationAggregates);

            calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            calculationEngineServiceTestsHelper
              .MockCalculationRepository
              .GetAssemblyBySpecificationId(Arg.Is(specificationId))
              .Returns(MockData.GetMockAssembly());

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
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
                .Returns(new ProviderResult()
                { });

            calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            ApiResponse<JobViewModel> jobViewModel = new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId });

            calculationEngineServiceTestsHelper
                .MockJobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModel);

            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("jobId", jobId);

            //Act
            await service.GenerateAllocations(message);

            //Assert
            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<CalculationAggregation>>());

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
        public async Task GenerateAllocations_GivenAValidRequestWhereIgnoreSaveProviderResultsFlagIsSet_ShouldNotSaveProviderResults()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;
            const string jobId = "job1";

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
                .MockCalculationRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

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
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
                .Returns(new ProviderResult());

            calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            ApiResponse<JobViewModel> jobViewModel = new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId });

            calculationEngineServiceTestsHelper
                .MockJobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModel);

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
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<CalculationAggregation>>());

            //Assert
            await
                calculationEngineServiceTestsHelper
                    .MockProviderResultRepo
                    .Received(0)
                    .SaveProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Any<int>());
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenJobIdMisingFromMessage_LogsErrorDoesNotAddJoblog()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;

            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("ignore-save-provider-results", "true");

            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            //Act
            Func<Task> action = async () => await service.GenerateAllocations(message);

            //Assert
            action
                .Should()
                .ThrowExactly<NonRetriableException>();

            await
                calculationEngineServiceTestsHelper
                    .MockJobsApiClient
                    .DidNotReceive()
                    .AddJobLog(Arg.Any<string>(), Arg.Any<JobLogUpdateModel>());

            calculationEngineServiceTestsHelper
                .MockLogger
                .Received(1)
                .Error("Missing job id for generating allocations");
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenRequestToRunButNotSave_EnsuresJobLogsAdded()
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

            ApiResponse<JobViewModel> jobViewModel = new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId });

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
                .MockCalculationRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

            calculationEngineServiceTestsHelper
                .MockJobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModel);

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
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
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
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<CalculationAggregation>>());

            //Assert
            await
                calculationEngineServiceTestsHelper
                    .MockJobsApiClient
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m => m.CompletedSuccessfully == null));

            await
                calculationEngineServiceTestsHelper
                    .MockJobsApiClient
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(
                        m => m.CompletedSuccessfully.Value &&
                             m.ItemsSucceeded == 20 &&
                             m.ItemsFailed == 0 &&
                             m.ItemsProcessed == 20 &&
                             m.Outcome == "20 provider results were generated successfully from 20 providers"));
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenCalculationResultsContainExcption_ThrowsNoRetriableExceptionEnsuresJobLogsAdded()
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

            ApiResponse<JobViewModel> jobViewModel = new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId });

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
                .MockCalculationRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

            calculationEngineServiceTestsHelper
                .MockJobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModel);

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
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
                .Returns(new ProviderResult()
                {
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            ExceptionMessage = "Exception occurred"
                        }
                    }
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
            Func<Task> test = async () => await service.GenerateAllocations(message);

            //Assert
            test
                .Should()
                .ThrowExactly<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Exceptions were thrown during generation of calculation results for specification Id: '{specificationId}'");

            await
                calculationEngineServiceTestsHelper
                    .MockJobsApiClient
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m =>
                        m.CompletedSuccessfully == false &&
                        m.Outcome == "Exceptions were thrown during generation of calculation results" &&
                        m.ItemsProcessed == 3));
        }

        [TestMethod]
        public void GenerateAllocations_GivenCalculationResultsContainExcptionButFailsToAddAJobLog_ThrowsNonRetriableExceptionAndLogsError()
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

            ApiResponse<JobViewModel> jobViewModel = new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId });

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
                .MockCalculationRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

            calculationEngineServiceTestsHelper
                .MockJobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModel);

            calculationEngineServiceTestsHelper
                .MockJobsApiClient
                .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m => m.ItemsProcessed == 3))
                .Returns((ApiResponse<JobLog>)null);

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
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
                .Returns(new ProviderResult()
                {
                    CalculationResults = new List<CalculationResult>
                    {
                        new CalculationResult
                        {
                            ExceptionMessage = "Exception occurred"
                        }
                    }
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
            Func<Task> test = async () => await service.GenerateAllocations(message);

            //Assert
            test
                .Should()
                .ThrowExactly<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Exceptions were thrown during generation of calculation results for specification Id: '{specificationId}'");

            calculationEngineServiceTestsHelper
                .MockLogger
                .Received(1)
                .Error(Arg.Is($"Failed to add a job log for job id '{jobId}'"));
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenMessgeCompletionStatusAlreadySet_LogsDoesNotUpdateJobLog()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const string jobId = "job-id-1";

            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            BuildProject buildProject = CreateBuildProject();

            ApiResponse<JobViewModel> jobViewModel = new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId, CompletionStatus = CompletionStatus.Superseded });

            calculationEngineServiceTestsHelper
                .MockJobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModel);

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

            //Assert
            await
                calculationEngineServiceTestsHelper
                    .MockJobsApiClient
                    .DidNotReceive()
                    .AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>());

            calculationEngineServiceTestsHelper
                .MockLogger
                .Received(1)
                .Information($"Received job with id: '{jobId}' is already in a completed state with status {jobViewModel.Content.CompletionStatus.ToString()}");
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenJobNotFound_LogsAndThrowsNonRetriableException()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const string jobId = "job-id-1";

            CalculationEngineServiceTestsHelper calculationEngineServiceTestsHelper =
                new CalculationEngineServiceTestsHelper();

            BuildProject buildProject = CreateBuildProject();

            calculationEngineServiceTestsHelper
                .MockJobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns((ApiResponse<JobViewModel>)null);

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
            Func<Task> test = async () => await service.GenerateAllocations(message);

            //Assert
            test
                .Should()
                .ThrowExactly<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Could not find the parent job with job id: '{jobId}'");

            await
                calculationEngineServiceTestsHelper
                    .MockJobsApiClient
                    .DidNotReceive()
                    .AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>());

            calculationEngineServiceTestsHelper
                .MockLogger
                .Received(1)
                .Error(Arg.Is($"Could not find the parent job with job id: '{jobId}'"));
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenJobIsGenerateCalculationAggregationsJob_EnsuresAggregationsCreatedAndCached()
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

            ApiResponse<JobViewModel> jobViewModel = new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId, JobDefinitionId = JobConstants.DefinitionNames.GenerateCalculationAggregationsJob });

            Dictionary<string, List<decimal>> cachedCalculationAggregates = new Dictionary<string, List<decimal>>
            {
                { "Calc1", new List<decimal>() },
                { "Calc2", new List<decimal>() },
                { "Calc3", new List<decimal>() }
            };

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
               .MockCacheProvider
               .GetAsync<Dictionary<string, List<decimal>>>($"{CacheKeys.CalculationAggregations}{specificationId}")
               .Returns(cachedCalculationAggregates);

            calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

            calculationEngineServiceTestsHelper
                .MockJobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModel);

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
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
                    .Returns(new ProviderResult
                    {
                        CalculationResults = new List<CalculationResult>
                        {
                            new CalculationResult { Value = 10, Calculation = new Common.Models.Reference { Name = "Calc1" } },
                            new CalculationResult { Value = 20, Calculation = new Common.Models.Reference { Name = "Calc2" } },
                            new CalculationResult { Value = 30, Calculation = new Common.Models.Reference { Name = "Calc3" } }
                        }
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
            messageUserProperties.Add("batch-count", "7");
            messageUserProperties.Add("calculations-to-aggregate", "Calc1,Calc2,Calc3");


            //Act
            await service.GenerateAllocations(message);

            //Assert
            calculationEngineServiceTestsHelper
               .MockCalculationEngine
               .Received(providerSummaries.Count)
               .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                   Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<CalculationAggregation>>());

            await
                calculationEngineServiceTestsHelper
                    .MockCacheProvider
                    .Received()
                    .SetAsync<Dictionary<string, List<decimal>>>(Arg.Any<string>(),
                        Arg.Is<Dictionary<string, List<decimal>>>(
                            m => m.Count == 3 &&
                                 m["Calc1"].Count == 20 &&
                                 m["Calc2"].Count == 20 &&
                                 m["Calc3"].Count == 20
                        ));

            await
                calculationEngineServiceTestsHelper
                    .MockJobsApiClient
                    .Received()
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m => m.CompletedSuccessfully == true));
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenCachedAggregateValuesExist_EnsuresAllocationModelCalledWithCachecdAggregates()
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

            Dictionary<string, List<decimal>> cachedCalculationAggregates = new Dictionary<string, List<decimal>>
            {
                { "Calc1", new List<decimal>{ 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 } },
                { "Calc2", new List<decimal>{ 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20 } },
                { "Calc3", new List<decimal>{ 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 } }
            };

            BuildProject buildProject = CreateBuildProject();

            ApiResponse<JobViewModel> jobViewModel = new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId });

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
                .MockCalculationRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

            calculationEngineServiceTestsHelper
             .MockCacheProvider
             .GetAsync<Dictionary<string, List<decimal>>>($"{CacheKeys.CalculationAggregations}{specificationId}_1")
             .Returns(cachedCalculationAggregates);

            calculationEngineServiceTestsHelper
                .MockJobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModel);

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
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
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
            messageUserProperties.Add("batch-count", "1");
            messageUserProperties.Add("batch-number", "1");
            messageUserProperties.Add("calculations-to-aggregate", "Calc1,Calc2,Calc3");

            //Act
            await service.GenerateAllocations(message);

            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Is<IEnumerable<CalculationAggregation>>(m =>
                        m.Count() == 3 &&
                        m.ElementAt(0).Values.ElementAt(0).Value == 200 &&
                        m.ElementAt(0).Values.ElementAt(1).Value == 10 &&
                        m.ElementAt(0).Values.ElementAt(2).Value == 10 &&
                        m.ElementAt(0).Values.ElementAt(3).Value == 10 &&
                        m.ElementAt(1).Values.ElementAt(0).Value == 400 &&
                        m.ElementAt(1).Values.ElementAt(1).Value == 20 &&
                        m.ElementAt(1).Values.ElementAt(2).Value == 20 &&
                        m.ElementAt(1).Values.ElementAt(3).Value == 20 &&
                        m.ElementAt(2).Values.ElementAt(0).Value == 600 &&
                        m.ElementAt(2).Values.ElementAt(1).Value == 30 &&
                        m.ElementAt(2).Values.ElementAt(2).Value == 30 &&
                        m.ElementAt(2).Values.ElementAt(3).Value == 30
                    ));

            //Assert
            await
                calculationEngineServiceTestsHelper
                    .MockJobsApiClient
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m => m.CompletedSuccessfully == null));

            await
                calculationEngineServiceTestsHelper
                    .MockJobsApiClient
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(
                        m => m.CompletedSuccessfully.Value &&
                             m.ItemsSucceeded == 20 &&
                             m.ItemsFailed == 0 &&
                             m.ItemsProcessed == 20 &&
                             m.Outcome == "20 provider results were generated successfully from 20 providers"));
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenCachedAggregateValuesDoesnotExist_EnsuresAggregationsAreIgnored()
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

            ApiResponse<JobViewModel> jobViewModel = new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId });

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
                .MockCalculationRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

            calculationEngineServiceTestsHelper
                .MockCacheProvider
                .GetAsync<Dictionary<string, List<decimal>>>($"{CacheKeys.CalculationAggregations}{specificationId}_1")
                .Returns((Dictionary<string, List<decimal>>)null);

            calculationEngineServiceTestsHelper
                .MockJobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModel);

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
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
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
            messageUserProperties.Add("batch-count", "1");
            messageUserProperties.Add("batch-number", "1");

            //Act
            await service.GenerateAllocations(message);

            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Is<IEnumerable<CalculationAggregation>>(m =>
                        !m.Any()
                    ));

            //Assert
            await
                calculationEngineServiceTestsHelper
                    .MockJobsApiClient
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m => m.CompletedSuccessfully == null));

            await
                calculationEngineServiceTestsHelper
                    .MockJobsApiClient
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(
                        m => m.CompletedSuccessfully.Value &&
                             m.ItemsSucceeded == 20 &&
                             m.ItemsFailed == 0 &&
                             m.ItemsProcessed == 20 &&
                             m.Outcome == "20 provider results were generated successfully from 20 providers"));
        }

        [TestMethod]
        public void GenerateAllocations_GivenAssemblyNotReturned_ThrowsRetriableException()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;
            const string jobId = "job1";

            Assembly assembly = typeof(CalculationEngineServiceTestsHelper).Assembly;

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
               .MockCalculationRepository
               .GetAssemblyBySpecificationId(Arg.Is(specificationId))
               .Returns((byte[])null);

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
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
                .Returns(new ProviderResult()
                {

                });

            calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            ApiResponse<JobViewModel> jobViewModel = new ApiResponse<JobViewModel>(HttpStatusCode.OK, new JobViewModel { Id = jobId });

            calculationEngineServiceTestsHelper
                .MockJobsApiClient
                .GetJobById(Arg.Is(jobId))
                .Returns(jobViewModel);

            CalculationEngineService service = calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("jobId", jobId);

            //Act
            Func<Task> test = async () => await service.GenerateAllocations(message);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Failed to get assembly for specification Id '{specificationId}'");
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
