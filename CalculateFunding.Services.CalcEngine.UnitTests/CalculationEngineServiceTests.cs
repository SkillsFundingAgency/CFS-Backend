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
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ApiClientSpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;
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
            _calculationEngineServiceTestsHelper = new CalculationEngineServiceTestsHelper();

            _specificationSummary = new ApiClientSpecificationSummary
            {
                DataDefinitionRelationshipIds = new[]
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
            const string jobId = "job1";

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<Dictionary<string, ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new CalculationResultContainer());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

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
                    Arg.Any<IDictionary<string, ProviderSourceDataset>>())
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
            messageUserProperties.Add("jobId", specificationId);

            await service.Run(message);

            await _calculationEngineServiceTestsHelper
                .MockProviderResultRepo
                .Received(0)
                .SaveProviderResults(
                Arg.Any<IEnumerable<ProviderResult>>(), 
                _specificationSummary, 
                partitionIndex, 
                partitionSize, 
                Arg.Any<Reference>(), 
                Arg.Any<string>(),
                jobId);
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

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<Dictionary<string, ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new CalculationResultContainer());

            _calculationEngineServiceTestsHelper
                .MockAssemblyService
                .GetAssemblyForSpecification(specificationId, null)
                .Returns(MockData.GetMockAssembly());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

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
                    Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>(),
                    Arg.Any<IEnumerable<string>>())
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
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(specificationId, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => new Dictionary<string, ProviderSourceDataset>()));

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-summary-cache-key", specificationSummaryCacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("jobId", jobId);

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

            //Act
            await service.Run(message);

            //Assert
            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>(),
                    Arg.Any<IEnumerable<string>>());

            await
            _calculationEngineServiceTestsHelper
                .MockProviderResultRepo
                .Received(7)
                .SaveProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Is(_specificationSummary), Arg.Is(partitionIndex), Arg.Is(partitionSize), Arg.Any<Reference>(), Arg.Any<string>(), Arg.Is(jobId));
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

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<Dictionary<string, ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new CalculationResultContainer());

            _calculationEngineServiceTestsHelper
                .MockAssemblyService
                .GetAssemblyForSpecification(specificationId, null)
                .Returns(MockData.GetMockAssembly());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            IEnumerable<CalculationAggregation> calculationAggregations = new[]
            {
                 new CalculationAggregation(),
                 new CalculationAggregation()
            };

            _calculationEngineServiceTestsHelper
                .MockCalculationAggregationService
                .BuildAggregations(Arg.Is<BuildAggregationRequest>(_ => _.SpecificationId == specificationId))
                .Returns(calculationAggregations);

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
                    Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>(),
                    Arg.Any<IEnumerable<string>>())
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
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(specificationId, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => new Dictionary<string, ProviderSourceDataset>()));

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
            await service.Run(message);

            //Assert
            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>(),
                    Arg.Any<IEnumerable<string>>());

            await
            _calculationEngineServiceTestsHelper
                .MockProviderResultRepo
                .Received(6)
                .SaveProviderResults(Arg.Is<IEnumerable<ProviderResult>>(m => m.Count() == 3), Arg.Is(_specificationSummary), Arg.Is(partitionIndex), Arg.Is(partitionSize), Arg.Any<Reference>(), Arg.Any<string>(), Arg.Is(jobId));

            await
            _calculationEngineServiceTestsHelper
                .MockProviderResultRepo
                .Received(1)
                .SaveProviderResults(Arg.Is<IEnumerable<ProviderResult>>(m => m.Count() == 2), Arg.Is(_specificationSummary), Arg.Is(partitionIndex), Arg.Is(partitionSize), Arg.Any<Reference>(), Arg.Any<string>(), Arg.Is(jobId));
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

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<Dictionary<string, ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new CalculationResultContainer());

            _calculationEngineServiceTestsHelper
                .MockAssemblyService
                .GetAssemblyForSpecification(specificationId, null)
                .Returns(MockData.GetMockAssembly());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

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
                    Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>(),
                    Arg.Any<IEnumerable<string>>())
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
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(specificationId, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => new Dictionary<string, ProviderSourceDataset>()));

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
            await service.Run(message);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>(),
                    Arg.Any<IEnumerable<string>>());

            //Assert
            await
                _calculationEngineServiceTestsHelper
                    .MockProviderResultRepo
                    .Received(0)
                    .SaveProviderResults(Arg.Any<IEnumerable<ProviderResult>>(), Arg.Is(_specificationSummary), Arg.Is(partitionIndex), Arg.Is(partitionSize), Arg.Any<Reference>(), Arg.Any<string>(), Arg.Is(jobId));
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

            JobViewModel jobViewModel = new JobViewModel { Id = jobId };

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<Dictionary<string, ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new CalculationResultContainer());

            _calculationEngineServiceTestsHelper
                .MockAssemblyService
                .GetAssemblyForSpecification(specificationId, null)
                .Returns(MockData.GetMockAssembly());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

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
                    Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>(),
                    Arg.Any<IEnumerable<string>>())
                .Returns(new ProviderResult()
                {

                });

            _calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            _calculationEngineServiceTestsHelper
                .MockDatasetRepo
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(specificationId, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => new Dictionary<string, ProviderSourceDataset>()));

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
            await service.Run(message);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>(),
                    Arg.Any<IEnumerable<string>>());

            //Assert
            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
                    .Received(1)
                    .UpdateJobStatus(Arg.Is(jobId), 0, 0, null, null);

            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
                    .Received(1)
                    .UpdateJobStatus(Arg.Is(jobId), 20, 0, true, "20 provider results were generated successfully from 20 providers");
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

            JobViewModel jobViewModel = new JobViewModel { Id = jobId };

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<Dictionary<string, ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new CalculationResultContainer());

            _calculationEngineServiceTestsHelper
                .MockAssemblyService
                .GetAssemblyForSpecification(specificationId, null)
                .Returns(MockData.GetMockAssembly());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

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
                    Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>(),
                    Arg.Any<IEnumerable<string>>())
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
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(specificationId, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => new Dictionary<string, ProviderSourceDataset>()));

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
            Func<Task> test = async () => await service.Run(message);

            test
                .Should()
                .ThrowExactly<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Exceptions were thrown during generation of calculation results for specification '{specificationId}'");

            //Assert
            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
                    .Received(1)
                    .UpdateJobStatus(Arg.Is(jobId), 20, 0, false, "Exceptions were thrown during generation of calculation results for specification 'spec1'");
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
            Func<Task> test = async () => await service.Run(message);

            //Assert
            test
                .Should()
                .ThrowExactly<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Received job with id: '{jobId}' is already in a completed state with status 'Superseded'");

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
            Func<Task> test = async () => await service.Run(message);

            //Assert
            test
                .Should()
                .ThrowExactly<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Could not find the job with id: '{jobId}'");

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

            JobViewModel jobViewModel = new JobViewModel { Id = jobId, JobDefinitionId = JobConstants.DefinitionNames.GenerateCalculationAggregationsJob };

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<Dictionary<string, ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new CalculationResultContainer());

            _calculationEngineServiceTestsHelper
                .MockAssemblyService
                .GetAssemblyForSpecification(specificationId, null)
                .Returns(MockData.GetMockAssembly());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

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
                    Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>(),
                    Arg.Any<IEnumerable<string>>())
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
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(specificationId, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => new Dictionary<string, ProviderSourceDataset>()));

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
            await service.Run(message);

            //Assert
            _calculationEngineServiceTestsHelper
               .MockCalculationEngine
               .Received(providerSummaries.Count)
               .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                   Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                   Arg.Any<IEnumerable<CalculationAggregation>>(),
                   Arg.Any<IEnumerable<string>>());

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
                    .UpdateJobStatus(Arg.Is(jobId), 20, 0, true, Arg.Any<string>());
        }

        [TestMethod]
        [DataRow("Calc1,Calc2,Calc3")]
        [DataRow("calc1,calc2,calc3")]
        [DataRow("cAlC1,calC2,CALC3")]
        public async Task GenerateAllocations_GivenCachedAggregateValuesExistAndAggregationsToAggregateInMessageAreInAnyCase_EnsuresAllocationModelCalledWithCachedAggregates(
            string calculationsToAggregate)
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationSummaryCacheKey = "specification-summary-cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;
            const string jobId = "job-id-1";

            JobViewModel jobViewModel = new JobViewModel { Id = jobId };

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<Dictionary<string, ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new CalculationResultContainer());

            _calculationEngineServiceTestsHelper
                .MockAssemblyService
                .GetAssemblyForSpecification(specificationId, null)
                .Returns(MockData.GetMockAssembly());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

            IEnumerable<CalculationAggregation> calculationAggregations = new List<CalculationAggregation>
            { 
                new CalculationAggregation(),
                new CalculationAggregation()
            };

            _calculationEngineServiceTestsHelper
                .MockCalculationAggregationService
                .BuildAggregations(Arg.Is<BuildAggregationRequest>(_ => _.SpecificationId == specificationId))
                .Returns(calculationAggregations);

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
                    Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>(),
                    Arg.Any<IEnumerable<string>>())
                .Returns(new ProviderResult()
                {

                });

            _calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            _calculationEngineServiceTestsHelper
                .MockDatasetRepo
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(specificationId, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => new Dictionary<string, ProviderSourceDataset>()));

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
            await service.Run(message);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(),
                    Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Is<IEnumerable<CalculationAggregation>>(m =>
                    m.SequenceEqual(calculationAggregations)
                    ),
                    Arg.Any<IEnumerable<string>>());

            //Assert
            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
                    .Received(1)
                    .UpdateJobStatus(Arg.Is(jobId), 20, 0, true, Arg.Any<string>());

            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
                    .Received(1)
                    .UpdateJobStatus(Arg.Is(jobId), 20, 0, true, "20 provider results were generated successfully from 20 providers");
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

            JobViewModel jobViewModel = new JobViewModel { Id = jobId };

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<Dictionary<string, ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new CalculationResultContainer());

            _calculationEngineServiceTestsHelper
                .MockAssemblyService
                .GetAssemblyForSpecification(specificationId, null)
                .Returns(MockData.GetMockAssembly());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

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
                    Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>(),
                    Arg.Any<IEnumerable<string>>())
                .Returns(new ProviderResult()
                {

                });

            _calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            _calculationEngineServiceTestsHelper
                .MockDatasetRepo
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(specificationId, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => new Dictionary<string, ProviderSourceDataset>()));

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
            await service.Run(message);

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Is<IEnumerable<CalculationAggregation>>(m =>
                        !m.Any()
                    ),
                    Arg.Any<IEnumerable<string>>());

            //Assert
            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
                    .Received(1)
                    .UpdateJobStatus(Arg.Is(jobId), 0, 0, null, null);

            await
                _calculationEngineServiceTestsHelper
                    .MockJobManagement
                    .Received(1)
                    .UpdateJobStatus(Arg.Is(jobId), 20, 0, true, "20 provider results were generated successfully from 20 providers");


            await _calculationEngineServiceTestsHelper.MockAssemblyService
                .Received(1)
                .GetAssemblyForSpecification(Arg.Is(specificationId), Arg.Is<string>(_ => _  == null));
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

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();
            mockAllocationModel
                .Execute(Arg.Any<Dictionary<string, ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new CalculationResultContainer());

            _calculationEngineServiceTestsHelper
                .MockAssemblyService
                .GetAssemblyForSpecification(specificationId, null)
                .Throws(new RetriableException($"Failed to get assembly for specification Id '{specificationId}'"));

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

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
                    Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>(),
                    Arg.Any<IEnumerable<string>>())
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
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(specificationId, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => new Dictionary<string, ProviderSourceDataset>()));

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
            Func<Task> test = async () => await service.Run(message);

            //Assert
            test
                .Should()
                .ThrowExactly<RetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Failed to get assembly for specification Id '{specificationId}'");

        }

        [TestMethod]
        public async Task UsesAssemblyFromProviderIfFound()
        {
            //Arrange
            const string cacheKey = "Cache-key";
            const string specificationSummaryCacheKey = "specification-summary-cache-key";
            const string specificationId = "spec1";
            const int partitionIndex = 0;
            const int partitionSize = 100;
            const int stop = partitionIndex + partitionSize - 1;
            const string jobId = "job-id-1";

            JobViewModel jobViewModel = new JobViewModel { Id = jobId };

            IList<ProviderSummary> providerSummaries = MockData.GetDummyProviders(20);

            IAllocationModel mockAllocationModel = Substitute.For<IAllocationModel>();

            mockAllocationModel
                .Execute(Arg.Any<Dictionary<string, ProviderSourceDataset>>(), Arg.Any<ProviderSummary>())
                .Returns(new CalculationResultContainer());

            _calculationEngineServiceTestsHelper
                .MockCacheProvider
                .ListRangeAsync<ProviderSummary>(cacheKey, partitionIndex, stop)
                .Returns(providerSummaries);

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
                    Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Any<IEnumerable<CalculationAggregation>>(),
                    Arg.Any<IEnumerable<string>>())
                .Returns(new ProviderResult());

            _calculationEngineServiceTestsHelper
                .MockEngineSettings
                .ProviderBatchSize = 3;

            _calculationEngineServiceTestsHelper
                .MockDatasetRepo
                .GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(specificationId, Arg.Any<IEnumerable<string>>(), Arg.Any<IEnumerable<string>>())
                .Returns(providerSummaries.ToDictionary(x => x.Id, x => new Dictionary<string, ProviderSourceDataset>()));

            CalculationEngineService service = _calculationEngineServiceTestsHelper.CreateCalculationEngineService();

            Message message = new Message();
            IDictionary<string, object> messageUserProperties = message.UserProperties;

            string etag = NewRandomString();

            messageUserProperties.Add("provider-summaries-partition-index", partitionIndex);
            messageUserProperties.Add("provider-summaries-partition-size", partitionSize);
            messageUserProperties.Add("provider-cache-key", cacheKey);
            messageUserProperties.Add("specification-summary-cache-key", specificationSummaryCacheKey);
            messageUserProperties.Add("specification-id", specificationId);
            messageUserProperties.Add("ignore-save-provider-results", "true");
            messageUserProperties.Add("jobId", jobId);
            messageUserProperties.Add("batch-count", "1");
            messageUserProperties.Add("batch-number", "1");
            messageUserProperties.Add("assembly-etag", etag);

            IAssemblyService assemblyProvider = _calculationEngineServiceTestsHelper.MockAssemblyService;

            assemblyProvider
                .GetAssemblyForSpecification(specificationId, etag)
                .Returns(MockData.GetMockAssembly());

            _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .GetAssemblyBySpecificationId(Arg.Is(specificationId))
                .Returns(MockData.GetMockAssembly());

            //Act
            await service.Run(message);

            await _calculationEngineServiceTestsHelper
                .MockCalculationRepository
                .Received(0)
                .GetAssemblyBySpecificationId(Arg.Is(specificationId));

            _calculationEngineServiceTestsHelper
                .MockCalculationEngine
                .Received(providerSummaries.Count)
                .CalculateProviderResults(mockAllocationModel, specificationId, calculationSummaryModelsReturn,
                    Arg.Any<ProviderSummary>(), Arg.Any<IDictionary<string, ProviderSourceDataset>>(),
                    Arg.Is<IEnumerable<CalculationAggregation>>(m =>
                        !m.Any()
                    ),
                    Arg.Any<IEnumerable<string>>());
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
