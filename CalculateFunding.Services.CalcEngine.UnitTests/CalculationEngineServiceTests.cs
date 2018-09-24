using System;
using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using Serilog;

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

       [TestMethod]
        public void 
           GenerateAllocations_GivenAValidRequestWhereSaveProviderResultsNotIgnored_ShouldBatchCorrectlyAndSaveProviderResults()
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
                .Execute(Arg.Any<List<ProviderSourceDataset>>())
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
                .GenerateAllocationModel(Arg.Any<BuildProject>())
                .Returns(mockAllocationModel);
            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>())
                .Returns((ProviderResult) null);

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

            service.GenerateAllocations(message).Wait();

            calculationEngineServiceTestsHelper
                .MockProviderResultRepo
                .Received(0)
                .SaveProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Any<int>());
        }

        [TestMethod]
        public void GenerateAllocations_GivenAValidRequestWhereNoResultsWereReturned_ShouldNotSaveAnything()
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
                .Execute(Arg.Any<List<ProviderSourceDataset>>())
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
                .GenerateAllocationModel(Arg.Any<BuildProject>())
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

            service.GenerateAllocations(message).Wait();

            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>());

            calculationEngineServiceTestsHelper
                .MockProviderResultRepo
                .Received(7)
                .SaveProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Any<int>());
        }

        [TestMethod]
        public void
            GenerateAllocations_GivenAValidRequestWhereIgnoreSaveProviderResultsFlagIsSet_ShouldNotSaveProviderResults()
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
                .Execute(Arg.Any<List<ProviderSourceDataset>>())
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
                .GenerateAllocationModel(Arg.Any<BuildProject>())
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

            service.GenerateAllocations(message).Wait();

            calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, buildProject, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>());

            calculationEngineServiceTestsHelper
                .MockProviderResultRepo
                .Received(0)
                .SaveProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Any<int>());
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
