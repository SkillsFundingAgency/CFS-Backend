using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.CalcEngine;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.CalcEngine.UnitTests;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ApiClientSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;
using Funding = CalculateFunding.Services.CalcEngine.Funding;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Calculator
{
    [TestClass]
    public class CalculationEngineServiceTests
    {
        private CalculationEngineServiceTestsHelper _calculationEngineServiceTestsHelper;
        private ApiClientSpecificationSummary _specificationSummary;
        private SpecificationSummary _cachedSummary;


        [TestInitialize]
        public void SetUp()
        {
            _calculationEngineServiceTestsHelper = new CalculationEngineServiceTestsHelper
            {
                MockEngineSettings =
                {
                    IsTestEngineEnabled = true
                }
            };

            _specificationSummary = new ApiClientSpecificationSummary
            {
                DataDefinitionRelationshipIds = new []
                {
                    NewRandomString(),
                    NewRandomString(),
                    NewRandomString()
                },
                FundingPeriod = new Reference
                {
                    Id = NewRandomString(),
                    Name = NewRandomString()
                },
                FundingStreams = new[] {
                    new Reference {
                        Id = NewRandomString(),
                        Name = NewRandomString()
                    }
                }
            };

            _cachedSummary = MockData.CreateSpecificationSummary();

            _calculationEngineServiceTestsHelper.MockCacheProvider
                .GetAsync<SpecificationSummary>(Arg.Any<string>())
                .Returns(_cachedSummary);
            _calculationEngineServiceTestsHelper.MockSpecificationsApiClient
                .GetSpecificationSummaryById(Arg.Any<string>())
                .Returns(new ApiResponse<ApiClientSpecificationSummary>(HttpStatusCode.OK, _specificationSummary));

            TemplateMapping mapping = new TemplateMapping
            {
                FundingStreamId = _cachedSummary.FundingStreams.Single().Id,
                SpecificationId = _cachedSummary.Id,
                TemplateMappingItems = new List<TemplateMappingItem>()
            };

            _calculationEngineServiceTestsHelper.MockCalculationRepository
                .GetTemplateMapping(_cachedSummary.Id, _cachedSummary.FundingStreams.Single().Id)
                .Returns(mapping);

            _calculationEngineServiceTestsHelper.MockPoliciesApiClient
                .GetFundingTemplateContents(_cachedSummary.FundingStreams.Single().Id, _cachedSummary.FundingPeriod.Id, _cachedSummary.TemplateIds[_cachedSummary.FundingStreams.Single().Id])
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, new TemplateMetadataContents { RootFundingLines = new FundingLine[0] }));

        }

        private string NewRandomString() => new RandomString();
        
        [Ignore("This test has a provider result as null, but should be checking successful results.")]
        [TestMethod]
        public async Task GenerateAllocations_GivenAValidRequestWhereSaveProviderResultsNotIgnored_ShouldBatchCorrectlyAndSaveProviderResults()
        {
            const string cacheKey = "Cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;

            BuildProject buildProject = CreateBuildProject();

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, Funding>>())
                .Returns(new List<CalculationResult>());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);
            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>())
                .Returns((ProviderResult)null);

            _calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-id", specificationId);

            await service.GenerateAllocations(message);

            await _calculationEngineServiceTestsHelper
                .MockProviderResultRepo
                .Received(0)
                .SaveProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), partitionIndex, partitionSize, Arg.Any<int>());
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenAValidRequestWhereNoResultsWereReturned_ShouldNotSaveAnything()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationSummaryCacheKey = "specification-summary-cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;
            const string jobId = "jobId";

            BuildProject buildProject = CreateBuildProject();

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, Funding>>())
                .Returns(new List<CalculationResult>());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            _calculationEngineServiceTestsHelper
               .MockCalculationRepository
               .GetAssemblyBySpecificationId(Arg.Is(specificationId))
               .Returns(MockData.GetMockAssembly());

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);
            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>())
                .Returns(new ProviderResult()
                {

                });

            _calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            JobViewModel jobViewModel = new JobViewModel { Id = jobId };

            _calculationEngineServiceTestsHelper
                .MockJobManagement
                .RetrieveJobAndCheckCanBeProcessed(Arg.Is(jobId))
                .Returns(jobViewModel);
            _calculationEngineServiceTestsHelper
                .MockDatasetRepo
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => Enumerable.Empty<ProviderSourceDataset>()));

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-summary-cache-key", specificationSummaryCacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("jobId", jobId);

            TemplateMapping mapping = new TemplateMapping { FundingStreamId = _cachedSummary.FundingStreams.Single().Id,
                SpecificationId = _cachedSummary.Id,
                TemplateMappingItems = new List<TemplateMappingItem>()
            };

            _calculationEngineServiceTestsHelper.MockCalculationRepository
                .GetTemplateMapping(_cachedSummary.Id, _cachedSummary.FundingStreams.Single().Id)
                .Returns(mapping);

            _calculationEngineServiceTestsHelper.MockPoliciesApiClient
                .GetFundingTemplateContents(_cachedSummary.FundingStreams.Single().Id, _cachedSummary.FundingPeriod.Id, _cachedSummary.TemplateIds[_cachedSummary.FundingStreams.Single().Id])
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, new TemplateMetadataContents { RootFundingLines = new FundingLine[0] }));

            //Act
            await service.GenerateAllocations(message);

            //Assert
            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(), Arg.Any<IEnumerable<CalculationAggregation>>());

            await
            _calculationEngineServiceTestsHelper
                .MockProviderResultRepo
                .Received(7)
                .SaveProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Is(partitionIndex), Arg.Is(partitionSize), Arg.Any<int>());
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenAValidRequestWhereNoResultsWereReturnedAndFeatureToggleIsEnabled_CallsSaveSevenTimes()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationSummaryCacheKey = "specification-summary-cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;
            const string jobId = "job1";

            BuildProject buildProject = CreateBuildProject();

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            Dictionary<string, List<decimal>> cachedCalculationAggregates = new Dictionary<string, List<decimal>>();

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, Funding>>())
                .Returns(new List<CalculationResult>());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            _calculationEngineServiceTestsHelper
              .MockCacheProvider
              .GetAsync<Dictionary<string, List<decimal>>>($"{CacheKeys.CalculationAggregations}{specificationId}")
              .Returns(cachedCalculationAggregates);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            _calculationEngineServiceTestsHelper
              .MockCalculationRepository
              .GetAssemblyBySpecificationId(Arg.Is(specificationId))
              .Returns(MockData.GetMockAssembly());

            IEnumerable<DatasetAggregation> datasetAggregations = new[]
            {
                 new DatasetAggregation(),
                 new DatasetAggregation()
            };

            _calculationEngineServiceTestsHelper
                .DatasetAggregationsRepository
                .GetDatasetAggregationsForSpecificationId(Arg.Is(specificationId))
                .Returns(datasetAggregations);

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
                .Returns(new ProviderResult()
                { });

            _calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            JobViewModel jobViewModel = new JobViewModel { Id = jobId };

            _calculationEngineServiceTestsHelper
                .MockJobManagement
                .RetrieveJobAndCheckCanBeProcessed(Arg.Is(jobId))
                .Returns(jobViewModel);

            _calculationEngineServiceTestsHelper
                .MockDatasetRepo
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => Enumerable.Empty<ProviderSourceDataset>()));

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-summary-cache-key", specificationSummaryCacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("jobId", jobId);

            //Act
            await service.GenerateAllocations(message);

            //Assert
            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(), Arg.Any<IEnumerable<CalculationAggregation>>());

            await
            _calculationEngineServiceTestsHelper
                .MockProviderResultRepo
                .Received(6)
                .SaveProviderResults(Arg.Is<IEnumerable<ProviderResult>>(m => m.Count() == 3), Arg.Is(partitionIndex), Arg.Is(partitionSize), Arg.Any<int>());

            await
            _calculationEngineServiceTestsHelper
                .MockProviderResultRepo
                .Received(1)
                .SaveProviderResults(Arg.Is<IEnumerable<ProviderResult>>(m => m.Count() == 2), Arg.Is(partitionIndex), Arg.Is(partitionSize), Arg.Any<int>());
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenAValidRequestWhereIgnoreSaveProviderResultsFlagIsSet_ShouldNotSaveProviderResults()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationSummaryCacheKey = "specification-summary-cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;
            const string jobId = "job1";

            BuildProject buildProject = CreateBuildProject();

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, Funding>>())
                .Returns(new List<CalculationResult>());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);
            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
                .Returns(new ProviderResult());

            _calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            JobViewModel jobViewModel = new JobViewModel { Id = jobId };

            _calculationEngineServiceTestsHelper
                .MockJobManagement
                .RetrieveJobAndCheckCanBeProcessed(Arg.Is(jobId))
                .Returns(jobViewModel);

            _calculationEngineServiceTestsHelper
                .MockDatasetRepo
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => Enumerable.Empty<ProviderSourceDataset>()));

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-summary-cache-key", specificationSummaryCacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("ignore-save-provider-results", "true");
            messageUserProperties.Add("jobId", jobId);

            //Act
            await service.GenerateAllocations(message);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(), Arg.Any<IEnumerable<CalculationAggregation>>());

            //Assert
            await
                _calculationEngineServiceTestsHelper
                    .MockProviderResultRepo
                    .Received(0)
                    .SaveProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), partitionIndex, partitionSize, Arg.Any<int>());
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenJobIdMisingFromMessage_LogsErrorDoesNotAddJoblog()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationSummaryCacheKey = "specification-summary-cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-summary-cache-key", specificationSummaryCacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("ignore-save-provider-results", "true");

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            //Act
            Func<Task> action = async () => await service.GenerateAllocations(message);

            //Assert
            action
                .Should()
                .ThrowExactly<NonRetriableException>();

            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
                    .DidNotReceive()
                    .AddJobLog(Arg.Any<string>(), Arg.Any<JobLogUpdateModel>());

            _calculationEngineServiceTestsHelper
                .MockLogger
                .Received(1)
                .Error("Missing job id for generating allocations");
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenRequestToRunButNotSave_EnsuresJobLogsAdded()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationSummaryCacheKey = "specification-summary-cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;
            const string jobId = "job-id-1";

            BuildProject buildProject = CreateBuildProject();

            JobViewModel jobViewModel = new JobViewModel { Id = jobId };

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, Funding>>())
                .Returns(new List<CalculationResult>());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

            _calculationEngineServiceTestsHelper
                .MockJobManagement
                .RetrieveJobAndCheckCanBeProcessed(Arg.Is(jobId))
                .Returns(jobViewModel);

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);
            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
                .Returns(new ProviderResult()
                {

                });

            _calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            _calculationEngineServiceTestsHelper
                .MockDatasetRepo
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => Enumerable.Empty<ProviderSourceDataset>()));

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-summary-cache-key", specificationSummaryCacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("ignore-save-provider-results", "true");
            messageUserProperties.Add("jobId", jobId);

            //Act
            await service.GenerateAllocations(message);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(), Arg.Any<IEnumerable<CalculationAggregation>>());

            //Assert
            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m => m.CompletedSuccessfully == null));

            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
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
            const string specificationSummaryCacheKey = "specification-summary-cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;
            const string jobId = "job-id-1";

            BuildProject buildProject = CreateBuildProject();

            JobViewModel jobViewModel = new JobViewModel { Id = jobId };

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, Funding>>())
                .Returns(new List<CalculationResult>());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

            _calculationEngineServiceTestsHelper
                .MockJobManagement
                .RetrieveJobAndCheckCanBeProcessed(Arg.Is(jobId))
                .Returns(jobViewModel);

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);
            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
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

            _calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            _calculationEngineServiceTestsHelper
                .MockDatasetRepo
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => Enumerable.Empty<ProviderSourceDataset>()));

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-summary-cache-key", specificationSummaryCacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("ignore-save-provider-results", "true");
            messageUserProperties.Add("jobId", jobId);

            //Act
            await service.GenerateAllocations(message);

            //Assert
            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m =>
                        m.CompletedSuccessfully == false &&
                        m.Outcome == "Exceptions were thrown during generation of calculation results" &&
                        m.ItemsProcessed == 20));
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenCalculationResultsContainExcptionButFailsToAddAJobLog_ThrowsNonRetriableExceptionAndLogsError()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationSummaryCacheKey = "specification-summary-cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;
            const string jobId = "job-id-1";

            BuildProject buildProject = CreateBuildProject();

            JobViewModel jobViewModel = new JobViewModel { Id = jobId };

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, Funding>>())
                .Returns(new List<CalculationResult>());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

            _calculationEngineServiceTestsHelper
                .MockJobManagement
                .RetrieveJobAndCheckCanBeProcessed(Arg.Is(jobId))
                .Returns(jobViewModel);

            _calculationEngineServiceTestsHelper
                .MockJobManagement
                .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m => m.ItemsProcessed == 3))
                .Returns((JobLog)null);

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);
            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
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

            _calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            _calculationEngineServiceTestsHelper
                .MockDatasetRepo
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => Enumerable.Empty<ProviderSourceDataset>()));

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-summary-cache-key", specificationSummaryCacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("ignore-save-provider-results", "true");
            messageUserProperties.Add("jobId", jobId);

            //Act
            await service.GenerateAllocations(message);

            //Assert
            _calculationEngineServiceTestsHelper
                .MockLogger
                .Received(1)
                .Error(Arg.Is($"Failed to add a job log for job id '{jobId}'"));
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenJobNotFound_Completed()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationSummaryCacheKey = "specification-summary-cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const string jobId = "job-id-1";

            JobViewModel jobViewModel = new JobViewModel { Id = jobId, CompletionStatus = CompletionStatus.Superseded };

            _calculationEngineServiceTestsHelper
                .MockJobManagement
                .RetrieveJobAndCheckCanBeProcessed(Arg.Is(jobId))
                .Throws(new JobAlreadyCompletedException(string.Empty, jobViewModel));

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-summary-cache-key", specificationSummaryCacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("ignore-save-provider-results", "true");
            messageUserProperties.Add("jobId", jobId);

            //Act
            await service.GenerateAllocations(message);

            //Assert
            
            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
                    .DidNotReceive()
                    .AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>());
        }

        [TestMethod]
        public async Task GenerateAllocations_GivenJobNotFound_LogsAndThrowsNonRetriableException()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationSummaryCacheKey = "specification-summary-cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const string jobId = "job-id-1";

            _calculationEngineServiceTestsHelper
                .MockJobManagement
                .RetrieveJobAndCheckCanBeProcessed(Arg.Is(jobId))
                .Throws(new JobNotFoundException(string.Empty, jobId));

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-summary-cache-key", specificationSummaryCacheKey);
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
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
                    .DidNotReceive()
                    .AddJobLog(Arg.Is(jobId), Arg.Any<JobLogUpdateModel>());
        }

        [TestMethod]
        [DataRow("Calc1,Calc2,Calc3")]
        [DataRow("calc1,calc2,calc3")]
        [DataRow("cAlC1,calC2,CALC3")]
        public async Task GenerateAllocations_GivenJobIsGenerateCalculationAggregationsJobAndCalculationsToAggregateInAnyCase_EnsuresAggregationsCreatedAndCached(string calculationsToAggregate)
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationSummaryCacheKey = "specification-summary-cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;
            const string jobId = "job-id-1";

            BuildProject buildProject = CreateBuildProject();

            JobViewModel jobViewModel = new JobViewModel { Id = jobId, JobDefinitionId = JobConstants.DefinitionNames.GenerateCalculationAggregationsJob };

            Dictionary<string, List<decimal>> cachedCalculationAggregates = new Dictionary<string, List<decimal>>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Calc1", new List<decimal>() },
                { "Calc2", new List<decimal>() },
                { "Calc3", new List<decimal>() }
            };

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, Funding>>())
                .Returns(new List<CalculationResult>());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            _calculationEngineServiceTestsHelper
               .MockCacheProvider
               .GetAsync<Dictionary<string, List<decimal>>>($"{CacheKeys.CalculationAggregations}{specificationId}")
               .Returns(cachedCalculationAggregates);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

            _calculationEngineServiceTestsHelper
                .MockJobManagement
                .RetrieveJobAndCheckCanBeProcessed(Arg.Is(jobId))
                .Returns(jobViewModel);

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
                    .Returns(new ProviderResult
                    {
                        CalculationResults = new List<CalculationResult>
                        {
                            new CalculationResult { Value = 10, Calculation = new Common.Models.Reference { Name = "Calc1" } },
                            new CalculationResult { Value = 20, Calculation = new Common.Models.Reference { Name = "Calc2" } },
                            new CalculationResult { Value = 30, Calculation = new Common.Models.Reference { Name = "Calc3" } }
                        }
                    });

            _calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            _calculationEngineServiceTestsHelper
                .MockDatasetRepo
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => Enumerable.Empty<ProviderSourceDataset>()));

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-summary-cache-key", specificationSummaryCacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("ignore-save-provider-results", "true");
            messageUserProperties.Add("jobId", jobId);
            messageUserProperties.Add("batch-count", "7");
            messageUserProperties.Add("calculations-to-aggregate", calculationsToAggregate);

            //Act
            await service.GenerateAllocations(message);

            //Assert
            _calculationEngineServiceTestsHelper
               .MockCalculationEngine
               .Received(providerSummaries.Count)
               .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                   Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                   Arg.Any<IDictionary<string, Funding>>(), Arg.Any<IEnumerable<CalculationAggregation>>());

            await
                _calculationEngineServiceTestsHelper
                    .MockCacheProvider
                    .Received()
                    .SetAsync(Arg.Any<string>(),
                        Arg.Is<Dictionary<string, List<object>>>(
                            m => m.Count == 3 &&
                                 m["Calc1"].Count == 20 &&
                                 m["Calc2"].Count == 20 &&
                                 m["Calc3"].Count == 20
                        ));

            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
                    .Received()
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m => m.CompletedSuccessfully == true));
        }

        [TestMethod]
        [DataRow("Calc1,Calc2,Calc3")]
        [DataRow("calc1,calc2,calc3")]
        [DataRow("cAlC1,calC2,CALC3")]
        public async Task GenerateAllocations_GivenCachedAggregateValuesExistAndAggregationsToAggregateInMessageAreInAnyCase_EnsuresAllocationModelCalledWithCachecdAggregates(string calculationsToAggregate)
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationSummaryCacheKey = "specification-summary-cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;
            const string jobId = "job-id-1";

            Dictionary<string, List<decimal>> cachedCalculationAggregates = new Dictionary<string, List<decimal>>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "Calc1", new List<decimal>{ 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 } },
                { "Calc2", new List<decimal>{ 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20, 20 } },
                { "Calc3", new List<decimal>{ 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30, 30 } }
            };

            BuildProject buildProject = CreateBuildProject();

            JobViewModel jobViewModel = new JobViewModel { Id = jobId };

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, Funding>>())
                .Returns(new List<CalculationResult>());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

            _calculationEngineServiceTestsHelper
             .MockCacheProvider
             .GetAsync<Dictionary<string, List<decimal>>>($"{CacheKeys.CalculationAggregations}{specificationId}_1")
             .Returns(cachedCalculationAggregates);

            _calculationEngineServiceTestsHelper
                .MockJobManagement
                .RetrieveJobAndCheckCanBeProcessed(Arg.Is(jobId))
                .Returns(jobViewModel);

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
                .Returns(new ProviderResult()
                {

                });

            _calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            _calculationEngineServiceTestsHelper
                .MockDatasetRepo
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => Enumerable.Empty<ProviderSourceDataset>()));

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("specification-summary-cache-key", specificationSummaryCacheKey);
            messageUserProperties.Add("ignore-save-provider-results", "true");
            messageUserProperties.Add("jobId", jobId);
            messageUserProperties.Add("batch-count", "1");
            messageUserProperties.Add("batch-number", "1");
            messageUserProperties.Add("calculations-to-aggregate", calculationsToAggregate);

            //Act
            await service.GenerateAllocations(message);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(), Arg.Is<IEnumerable<CalculationAggregation>>(m =>
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
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m => m.CompletedSuccessfully == null));

            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
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
            const string specificationSummaryCacheKey = "specification-summary-cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;
            const string jobId = "job-id-1";

            BuildProject buildProject = CreateBuildProject();

            JobViewModel jobViewModel = new JobViewModel { Id = jobId };

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, Funding>>())
                .Returns(new List<CalculationResult>());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .GetAsync<Dictionary<string, List<decimal>>>($"{CacheKeys.CalculationAggregations}{specificationId}_1")
                .Returns((Dictionary<string, List<decimal>>)null);

            _calculationEngineServiceTestsHelper
                .MockJobManagement
                .RetrieveJobAndCheckCanBeProcessed(Arg.Is(jobId))
                .Returns(jobViewModel);

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
                .Returns(new ProviderResult()
                {

                });

            _calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            _calculationEngineServiceTestsHelper
                .MockDatasetRepo
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => Enumerable.Empty<ProviderSourceDataset>()));

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-summary-cache-key", specificationSummaryCacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("ignore-save-provider-results", "true");
            messageUserProperties.Add("jobId", jobId);
            messageUserProperties.Add("batch-count", "1");
            messageUserProperties.Add("batch-number", "1");

            //Act
            await service.GenerateAllocations(message);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(), Arg.Is<IEnumerable<CalculationAggregation>>(m =>
                        !m.Any()
                    ));

            //Assert
            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
                    .Received(1)
                    .AddJobLog(Arg.Is(jobId), Arg.Is<JobLogUpdateModel>(m => m.CompletedSuccessfully == null));

            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
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
            const string specificationSummaryCacheKey = "specification-summary-cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;
            const string jobId = "job1";

            BuildProject buildProject = CreateBuildProject();

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<List<ProviderSourceDataset>>(), Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, Funding>>())
                .Returns(new List<CalculationResult>());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetBuildProjectBySpecificationId(Arg.Any<string>())
                .Returns(buildProject);

            _calculationEngineServiceTestsHelper
               .MockCalculationRepository
               .GetAssemblyBySpecificationId(Arg.Is(specificationId))
               .Returns((byte[])null);

            IList<CalculationSummaryModel> calculationSummaryModelsReturn = CreateDummyCalculationSummaryModels();
            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetCalculationSummariesForSpecification(specificationId)
                .Returns(calculationSummaryModelsReturn);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .GenerateAllocationModel(Arg.Any<Assembly>())
                .Returns(mockAllocationModel);
            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Is<ProviderSummary>(summary => providerSummaries.Contains(summary)),
                    Arg.Any<IEnumerable<ProviderSourceDataset>>(),
                    Arg.Any<IDictionary<string, Funding>>(), Arg.Any<IEnumerable<CalculationAggregation>>())
                .Returns(new ProviderResult()
                {

                });

            _calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            JobViewModel jobViewModel = new JobViewModel { Id = jobId };

            _calculationEngineServiceTestsHelper
                .MockJobManagement
                .RetrieveJobAndCheckCanBeProcessed(Arg.Is(jobId))
                .Returns(jobViewModel);

            _calculationEngineServiceTestsHelper
                .MockDatasetRepo
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => Enumerable.Empty<ProviderSourceDataset>()));

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-summary-cache-key", specificationSummaryCacheKey);
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
                    CalculationType = CalculationType.Template,
                    Id = "TC1",
                    CalculationValueType = CalculationValueType.Number
                },
                new CalculationSummaryModel()
                {
                    Name = "TestCalc2",
                    CalculationType = CalculationType.Template,
                    Id = "TC2",
                    CalculationValueType = CalculationValueType.Number
                }
            };
            return calculationSummaryModels;
        }
    }
}
