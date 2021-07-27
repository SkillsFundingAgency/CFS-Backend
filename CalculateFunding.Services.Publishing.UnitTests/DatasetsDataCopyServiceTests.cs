using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Excel;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class DatasetsDataCopyServiceTests
    {
        private readonly Mock<IJobManagement> _jobManagement;
        private readonly Mock<ILogger> _logger;
        private readonly Mock<IDatasetsApiClient> _datasetsApiClient;
        private readonly Mock<ISpecificationService> _specificationService;
        private readonly Mock<IPublishedFundingRepository> _publishedFundingRepository;
        private readonly Mock<IRelationshipDataExcelWriter> _excelWriter;

        private readonly IDatasetsDataCopyService _service;

        public DatasetsDataCopyServiceTests()
        {
            _jobManagement = new Mock<IJobManagement>();
            _logger = new Mock<ILogger>();
            _datasetsApiClient = new Mock<IDatasetsApiClient>();
            _specificationService = new Mock<ISpecificationService>();
            _publishedFundingRepository = new Mock<IPublishedFundingRepository>();
            _excelWriter = new Mock<IRelationshipDataExcelWriter>();

            IPublishingResiliencePolicies policies = PublishingResilienceTestHelper.GenerateTestPolicies();

            _service = new DatasetsDataCopyService(
                                        _jobManagement.Object,
                                        _logger.Object,
                                        _datasetsApiClient.Object,
                                        _specificationService.Object,
                                        policies,
                                        _publishedFundingRepository.Object,
                                        _excelWriter.Object);
        }

        [TestMethod]
        public async Task Process_GivenNoSpecificationIdOnMessage_ThrowsException()
        {
            // Arrange
            Message message = CreateMessage();

            // Act
            Func<Task> result = async () => await _service.Process(message);

            // Assert
            result
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .Message
                .Contains("specification-id");
        }

        [TestMethod]
        public async Task Process_GivenNoRelationshipIdOnMessage_ThrowsException()
        {
            // Arrange
            string specificationId = NewRandomString();
            Message message = CreateMessage(specificationId);

            // Act
            Func<Task> result = async () => await _service.Process(message);

            // Assert
            result
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .Message
                .Contains("relationship-id");
        }

        [TestMethod]
        public async Task Process_GivenNoRelationshipFound_ThrowsException()
        {
            // Arrange
            string specificationId = NewRandomString();
            string relationshipId = NewRandomString();
            Message message = CreateMessage(specificationId, relationshipId);

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationships = new[]
            {
                NewDatasetSpecificationRelationshipViewModel(_ => _.WithId(NewRandomString()))
            };

            GivenRelationshipsForSpecification(specificationId, relationships);
            AndSpecificationSummaryForSpecificaiton(specificationId, NewSpecificationSummary());

            // Act
            Func<Task> result = async () => await _service.Process(message);

            // Assert
            result
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be($"No relationship found for the specificaiton id - {specificationId} and relationship id - {relationshipId}.");

            _specificationService.Verify(x => x.GetSpecificationSummaryById(specificationId), Times.Once);
            _datasetsApiClient.Verify(x => x.GetCurrentRelationshipsBySpecificationId(specificationId), Times.Once);
        }

        [TestMethod]
        public async Task Process_GivenNoSpecificationFound_ThrowsException()
        {
            // Arrange
            string specificationId = NewRandomString();
            string relationshipId = NewRandomString();
            Message message = CreateMessage(specificationId, relationshipId);

            GivenNoSpecificationSummaryForSpecificaiton(specificationId);

            // Act
            Func<Task> result = async () => await _service.Process(message);

            // Assert
            result
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Specification not found for specification id- {specificationId}");

            _specificationService.Verify(x => x.GetSpecificationSummaryById(specificationId), Times.Once);
        }

        [TestMethod]
        public async Task Process_GivenDatasetFileIsFailedToUplaod_ThrowsException()
        {
            // Arrange
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateId = NewRandomString();
            string relationshipId = NewRandomString();
            string relationshipName = NewRandomString().Substring(0, 30); // Used for excel worksheet name

            string Ukprn1 = NewRandomString();
            string fundingLine1 = NewRandomString();
            uint fundingLineTemplateId1 = NewRandomUint();
            decimal fundingLineValue1 = NewRandomDecimal();
            
            string datasetId = NewRandomString();
            int datasetVersionVersion = NewRandomInt();
            string fileName = NewRandomString();

            Message message = CreateMessage(specificationId, relationshipId);

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationships = new[]
            {
                NewDatasetSpecificationRelationshipViewModel(_ =>
                _.WithId(relationshipId)
                 .WithName(relationshipName)
                 .WithDatasetId(datasetId)
                 .WithPublishedSpecificationConfiguration(NewPublishedSpecificationConfiguration(c => c
                                    .WithFundingLines(
                                        NewPublishedSpecificationItem(f => f.WithName(fundingLine1).WithTemplateId(fundingLineTemplateId1))))
                 ))
            };

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithFundingStreamIds(fundingStreamId)
                                                                                       .WithFundingPeriodId(fundingPeriodId)
                                                                                       .WithTemplateIds((fundingPeriodId, templateId)));

            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                NewPublishedProvider(pp => pp.WithReleased(
                    NewPublishedProviderVersion(_ => _.WithProvider(NewProvider(p => p.WithUKPRN(Ukprn1)))
                                                  .WithFundingLines(NewFundingLine(f => f.WithTemplateLineId(fundingLineTemplateId1).WithValue(fundingLineValue1))))))

            };

            IEnumerable<RelationshipDataSetExcelData> expectedDatasetData = new[]
            {
                new RelationshipDataSetExcelData(Ukprn1)
                {
                    FundingLines = new Dictionary<string, decimal?> { { $"FL_{fundingLineTemplateId1}_{fundingLine1}", fundingLineValue1 } }
                }
            };

            NewDatasetVersionResponseModel datasetVersionResponse = new NewDatasetVersionResponseModel()
            {
                DatasetId = datasetId,
                Version = datasetVersionVersion,
                Filename = fileName,
                FundingStreamId = fundingStreamId
            };

            byte[] excelData = new byte[0];

            GivenRelationshipsForSpecification(specificationId, relationships);
            AndSpecificationSummaryForSpecificaiton(specificationId, specificationSummary);
            AndPublishProvidersForSpecification(specificationId, publishedProviders);
            GivenDatasetVersionUpdate(datasetId, relationshipName, fundingStreamId, datasetVersionResponse);
            GivenRelationshipExcelWriterReturnExcelForDatasetData(relationshipName, excelData);
            GivenFailedToUploadOfDatasetFile(datasetVersionResponse, HttpStatusCode.BadRequest);

            // Act
            Func<Task> result = async () => await _service.Process(message);

            // Assert
            result
               .Should()
               .Throw<Exception>()
               .Which
               .Message
               .Should()
               .Be($"Failed to upload the dataset file, dataset id - {datasetId}, file name - {fileName}. Status Code - {HttpStatusCode.BadRequest}");

            AssertRelationshipsRetrievedBySpecificationId(specificationId);
            AssertSpecificationSummaryRetrievedBySpecificationId(specificationId);
            AssertPublishedProvidersForSpecificationRetrievedForBatchProcessing(specificationId);
            AssertDatasetVersionUpdateHasBeenCalled(fundingStreamId, relationshipName, datasetId);
            AssertExcelDatasetData(relationshipName, expectedDatasetData, Ukprn1);
            AssertDatasetFileUpload(datasetVersionResponse);
            AssertThatRelationshipUpdatedNotCalled(relationshipId, datasetId, datasetVersionVersion);
        }

        [TestMethod]
        public async Task Process_GivenValidProvidersData_ShouldCreateNewDatasetVersionAndUpdateTheRelationshipWithNewDatasetVerion()
        {
            // Arrange
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateId = NewRandomString();
            string relationshipId = NewRandomString();
            string relationshipName = NewRandomString().Substring(0, 30); // Used for excel worksheet name

            string Ukprn1 = NewRandomString();
            string Ukprn2 = NewRandomString();
            string Ukprn3 = NewRandomString();
            string fundingLine1 = NewRandomString();
            string fundingLine2 = NewRandomString();
            uint fundingLineTemplateId1 = NewRandomUint();
            uint fundingLineTemplateId2 = NewRandomUint();
            decimal fundingLineValue1 = NewRandomDecimal();
            decimal fundingLineValue2 = NewRandomDecimal();
            string calculation1 = NewRandomString();
            string calculation2 = NewRandomString();
            uint calculationTemplateId1 = NewRandomUint();
            uint calculationTemplateId2 = NewRandomUint();
            decimal calculationValue1 = NewRandomDecimal();
            decimal calculationValue2 = NewRandomDecimal();

            string datasetId = NewRandomString();
            int datasetVersionVersion = NewRandomInt();
            string fileName = NewRandomString();

            Message message = CreateMessage(specificationId, relationshipId);

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationships = new[]
            {
                NewDatasetSpecificationRelationshipViewModel(_ =>
                _.WithId(relationshipId)
                 .WithName(relationshipName)
                 .WithDatasetId(datasetId)
                 .WithPublishedSpecificationConfiguration(NewPublishedSpecificationConfiguration(c => c
                                    .WithFundingLines(
                                        NewPublishedSpecificationItem(f => f.WithName(fundingLine1).WithTemplateId(fundingLineTemplateId1)),
                                        NewPublishedSpecificationItem(f => f.WithName(fundingLine2).WithTemplateId(fundingLineTemplateId2)))
                                    .WithCalculations(
                                        NewPublishedSpecificationItem(f => f.WithName(calculation1).WithTemplateId(calculationTemplateId1)),
                                        NewPublishedSpecificationItem(f => f.WithName(calculation2).WithTemplateId(calculationTemplateId2)))
                                    )
                 ))
            };

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithFundingStreamIds(fundingStreamId)
                                                                                       .WithFundingPeriodId(fundingPeriodId)
                                                                                       .WithTemplateIds((fundingPeriodId, templateId)));

            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                NewPublishedProvider(p => p.WithReleased(
                    NewPublishedProviderVersion(_ => _.WithProvider(NewProvider(p => p.WithUKPRN(Ukprn1)))
                                                  .WithFundingLines(NewFundingLine(f => f.WithTemplateLineId(fundingLineTemplateId1).WithValue(fundingLineValue1)),
                                                                    NewFundingLine(f => f.WithTemplateLineId(fundingLineTemplateId2).WithValue(fundingLineValue2)))
                                                  .WithFundingCalculations(NewFundingCalculation(c => c.WithTemplateCalculationId(calculationTemplateId2).WithValue(calculationValue2)))))),
                NewPublishedProvider(p => p.WithReleased(
                    NewPublishedProviderVersion(_ => _.WithProvider(NewProvider(p => p.WithUKPRN(Ukprn2)))
                                                  .WithFundingLines(NewFundingLine(f => f.WithTemplateLineId(fundingLineTemplateId2).WithValue(fundingLineValue2)))
                                                  .WithFundingCalculations(NewFundingCalculation(c => c.WithTemplateCalculationId(calculationTemplateId1).WithValue(calculationValue1)))))),
                NewPublishedProvider(p => p.WithReleased(
                    NewPublishedProviderVersion(_ => _.WithProvider(NewProvider(p => p.WithUKPRN(Ukprn3)))
                                                  .WithFundingLines(NewFundingLine(f => f.WithTemplateLineId(fundingLineTemplateId1).WithValue(fundingLineValue1)))
                                                  .WithFundingCalculations(NewFundingCalculation(c => c.WithTemplateCalculationId(calculationTemplateId1).WithValue(calculationValue1)),
                                                                           NewFundingCalculation(c => c.WithTemplateCalculationId(calculationTemplateId2).WithValue(calculationValue2))))))

            };

            IEnumerable<RelationshipDataSetExcelData> expectedDatasetData = new[]
            {
                new RelationshipDataSetExcelData(Ukprn1)
                {
                    FundingLines = new Dictionary<string, decimal?> { { $"FL_{fundingLineTemplateId1}_{fundingLine1}", fundingLineValue1 }, { $"FL_{fundingLineTemplateId2}_{fundingLine2}", fundingLineValue2 } },
                    Calculations = new Dictionary<string, decimal?>{ { $"Calc_{calculationTemplateId1}_{calculation1}", null }, { $"Calc_{calculationTemplateId2}_{calculation2}", calculationValue2 } }
                },
                new RelationshipDataSetExcelData(Ukprn2)
                {
                    FundingLines = new Dictionary<string, decimal?> { { $"FL_{fundingLineTemplateId1}_{fundingLine1}", null }, { $"FL_{fundingLineTemplateId2}_{fundingLine2}", fundingLineValue2 } },
                    Calculations = new Dictionary<string, decimal?>{ { $"Calc_{calculationTemplateId1}_{calculation1}", calculationValue1 }, { $"Calc_{calculationTemplateId2}_{calculation2}", null } }
                },
                new RelationshipDataSetExcelData(Ukprn3)
                {
                    FundingLines = new Dictionary<string, decimal?> { { $"FL_{fundingLineTemplateId1}_{fundingLine1}", fundingLineValue1 }, { $"FL_{fundingLineTemplateId2}_{fundingLine2}", null } },
                    Calculations = new Dictionary<string, decimal?>{ { $"Calc_{calculationTemplateId1}_{calculation1}", calculationValue1 }, { $"Calc_{calculationTemplateId2}_{calculation2}", calculationValue2 } }
                }
            };

            NewDatasetVersionResponseModel datasetVersionResponse = new NewDatasetVersionResponseModel()
            {
                DatasetId = datasetId,
                Version = datasetVersionVersion,
                Filename = fileName,
                FundingStreamId = fundingStreamId
            };

            byte[] excelData = new byte[0];

            GivenRelationshipsForSpecification(specificationId, relationships);
            AndSpecificationSummaryForSpecificaiton(specificationId, specificationSummary);
            AndPublishProvidersForSpecification(specificationId, publishedProviders);
            GivenDatasetVersionUpdate(datasetId, relationshipName, fundingStreamId, datasetVersionResponse);
            GivenRelationshipExcelWriterReturnExcelForDatasetData(relationshipName, excelData);
            AndSuccessfulUploadOfDatasetFile(datasetVersionResponse);
            AndAssignDatasetVersionToRelationship(datasetId, relationshipId, datasetVersionVersion);

            // Act
            await _service.Process(message);

            // Assert
            AssertRelationshipsRetrievedBySpecificationId(specificationId);
            AssertSpecificationSummaryRetrievedBySpecificationId(specificationId);
            AssertPublishedProvidersForSpecificationRetrievedForBatchProcessing(specificationId);
            AssertDatasetVersionUpdateHasBeenCalled(fundingStreamId, relationshipName, datasetId);
            AssertExcelDatasetData(relationshipName, expectedDatasetData, Ukprn1, Ukprn2, Ukprn3);
            AssertDatasetFileUpload(datasetVersionResponse);
            AssertThatRelationshipUpdatedWithNewDatasetVersion(relationshipId, datasetId, datasetVersionVersion);
        }

        private void AssertThatRelationshipUpdatedWithNewDatasetVersion(string relationshipId, string datasetId, int datasetVersionVersion)
        {
            _datasetsApiClient.Verify(x => x.AssignDatasourceVersionToRelationship(
                It.Is<AssignDatasourceModel>(m => m.DatasetId == datasetId &&
                                                  m.RelationshipId == relationshipId &&
                                                  m.Version == datasetVersionVersion)), Times.Once);
        }

        private void AssertThatRelationshipUpdatedNotCalled(string relationshipId, string datasetId, int datasetVersionVersion)
        {
            _datasetsApiClient.Verify(x => x.AssignDatasourceVersionToRelationship(
                It.Is<AssignDatasourceModel>(m => m.DatasetId == datasetId &&
                                                  m.RelationshipId == relationshipId &&
                                                  m.Version == datasetVersionVersion)), Times.Never);
        }

        private void AssertDatasetFileUpload(NewDatasetVersionResponseModel datasetVersionResponse)
        {
            _datasetsApiClient.Verify(x => x.UploadDatasetFile(
                           It.Is<string>(fileName => fileName == datasetVersionResponse.Filename),
                           It.Is<DatasetMetadataViewModel>(m => m.DataDefinitionId == datasetVersionResponse.DefinitionId &&
                                                                m.DatasetId == datasetVersionResponse.DatasetId &&
                                                                m.Filename == datasetVersionResponse.Filename &&
                                                                m.FundingStreamId == datasetVersionResponse.FundingStreamId)), Times.Once);
        }

        private void AssertExcelDatasetData(string relationshipName, IEnumerable<RelationshipDataSetExcelData> expectedDatasetData, string providerUkprn1, string providerUkprn2 = null, string providerUkprn3 = null)
        {
            _excelWriter.Verify(x => x.WriteToExcel(
                            It.Is<string>(a => a == relationshipName),
                            It.Is<IEnumerable<RelationshipDataSetExcelData>>(d =>
                                                AreEqual(d.Single(a => a.Ukprn == providerUkprn1), expectedDatasetData.Single(e => e.Ukprn == providerUkprn1)) &&
                                                (providerUkprn2 == null || AreEqual(d.Single(a => a.Ukprn == providerUkprn2), expectedDatasetData.Single(e => e.Ukprn == providerUkprn2))) &&
                                                (providerUkprn3 == null || AreEqual(d.Single(a => a.Ukprn == providerUkprn3), expectedDatasetData.Single(e => e.Ukprn == providerUkprn3)))))
            , Times.Once);
        }

        private void AssertDatasetVersionUpdateHasBeenCalled(string fundingStreamId, string relationshipName, string datasetId)
        {
            _datasetsApiClient.Verify(x => x.DatasetVersionUpdate(
                             It.Is<DatasetVersionUpdateModel>(x => x.DatasetId == datasetId && x.Filename == relationshipName && x.FundingStreamId == fundingStreamId)), Times.Once);
        }

        private void AssertPublishedProvidersForSpecificationRetrievedForBatchProcessing(string specificationId)
        {
            _publishedFundingRepository.Verify(x => x.PublishedProviderBatchProcessing("IS_NULL(c.content.released) = false",
                    specificationId,
                    It.IsAny<Func<List<PublishedProvider>, Task>>(),
                    100,
                    null,
                    null)
            , Times.Once);
        }

        private void AssertSpecificationSummaryRetrievedBySpecificationId(string specificationId)
        {
            _specificationService.Verify(x => x.GetSpecificationSummaryById(specificationId), Times.Once);
        }

        private void AssertRelationshipsRetrievedBySpecificationId(string specificationId)
        {
            _datasetsApiClient.Verify(x => x.GetCurrentRelationshipsBySpecificationId(specificationId), Times.Once);
        }

        private bool AreEqual(RelationshipDataSetExcelData actual, RelationshipDataSetExcelData expected)
        {
            var equal = actual.Ukprn == expected.Ukprn &&
                actual.FundingLines.Count == expected.FundingLines.Count &&
                actual.Calculations.Count == expected.Calculations.Count;

            if (!equal) return false;

            foreach (KeyValuePair<string, decimal?> actualFundingLine in actual.FundingLines)
            {
                if (expected.FundingLines.TryGetValue(actualFundingLine.Key, out decimal? expectedValue) && actualFundingLine.Value == expectedValue) continue;
                else return false;
            }

            foreach (KeyValuePair<string, decimal?> actualCalculation in actual.Calculations)
            {
                if (expected.Calculations.TryGetValue(actualCalculation.Key, out decimal? expectedValue) && actualCalculation.Value == expectedValue) continue;
                else return false;
            }

            return true;
        }

        private void AndAssignDatasetVersionToRelationship(string datasetId, string relationshipId, int datasetVersionVersion)
        {
            _datasetsApiClient.Setup(x => x.AssignDatasourceVersionToRelationship(
                It.Is<AssignDatasourceModel>(m => m.DatasetId == datasetId &&
                                                  m.RelationshipId == relationshipId &&
                                                  m.Version == datasetVersionVersion)))
            .ReturnsAsync(new ApiResponse<Common.ApiClient.Datasets.Models.JobCreationResponse>(HttpStatusCode.OK, 
                                new Common.ApiClient.Datasets.Models.JobCreationResponse(), null));
        }

        private void AndSuccessfulUploadOfDatasetFile(NewDatasetVersionResponseModel datasetVersionResponse)
        {
            _datasetsApiClient.Setup(x => x.UploadDatasetFile(
                It.Is<string>(fileName => fileName == datasetVersionResponse.Filename),
                It.Is<DatasetMetadataViewModel>(m => m.DatasetId == datasetVersionResponse.DatasetId &&
                                                     m.Filename == datasetVersionResponse.Filename &&
                                                     m.FundingStreamId == datasetVersionResponse.FundingStreamId)))
                .ReturnsAsync(HttpStatusCode.OK);
        }

        private void GivenFailedToUploadOfDatasetFile(NewDatasetVersionResponseModel datasetVersionResponse, HttpStatusCode responseCode)
        {
            _datasetsApiClient.Setup(x => x.UploadDatasetFile(
                It.Is<string>(fileName => fileName == datasetVersionResponse.Filename),
                It.Is<DatasetMetadataViewModel>(m => m.DataDefinitionId == datasetVersionResponse.DefinitionId &&
                                                     m.DatasetId == datasetVersionResponse.DatasetId &&
                                                     m.Filename == datasetVersionResponse.Filename &&
                                                     m.FundingStreamId == datasetVersionResponse.FundingStreamId)))
                .ReturnsAsync(responseCode);
        }

        private void GivenRelationshipExcelWriterReturnExcelForDatasetData(string relationshipName, byte[] excelData)
        {
            _excelWriter.Setup(x => x.WriteToExcel(It.Is<string>(a => a == relationshipName), It.IsAny<IEnumerable<RelationshipDataSetExcelData>>()))
            .Returns(excelData);
        }

        private void GivenDatasetVersionUpdate(string datasetId, string relationshipName, string fundingStreamId, NewDatasetVersionResponseModel datasetVersionResponse)
        {
            _datasetsApiClient.Setup(x => x.DatasetVersionUpdate(It.Is<DatasetVersionUpdateModel>(x => x.DatasetId == datasetId && x.Filename == relationshipName && x.FundingStreamId == fundingStreamId)))
                 .ReturnsAsync(new ValidatedApiResponse<NewDatasetVersionResponseModel>(HttpStatusCode.OK, datasetVersionResponse));
        }

        private void AndPublishProvidersForSpecification(string specificationId, IEnumerable<PublishedProvider> publishedProviders)
        {
            IEnumerable<PublishedProvider> publishProvidersOne = publishedProviders.Take(1);
            IEnumerable<PublishedProvider> publishedProvidersTwo = publishedProviders.Skip(1).ToList();

            _publishedFundingRepository.Setup(_ => _.PublishedProviderBatchProcessing("IS_NULL(c.content.released) = false",
                    specificationId,
                    It.IsAny<Func<List<PublishedProvider>, Task>>(),
                    100,
                    null,
                    null))
                .Callback<string, string, Func<List<PublishedProvider>, Task>, int, string, string>((pred, spec,
                    batchProcessor, batchSize, joinPred, flc) =>
                {
                    batchProcessor(publishProvidersOne.ToList())
                        .GetAwaiter()
                        .GetResult();

                    batchProcessor(publishedProvidersTwo.ToList())
                        .GetAwaiter()
                        .GetResult();
                })
                .Returns(Task.CompletedTask);
        }

        private IEnumerable<T> ReadFromQueue<T>(Queue<T> queue, int count)
        {
            while(count > 0 && queue.Count > 0)
            {
                yield return queue.Dequeue();
                count--;
            }
        }

        private void GivenRelationshipsForSpecification(string specificationId, IEnumerable<DatasetSpecificationRelationshipViewModel> relationships)
        {
            _datasetsApiClient.Setup(x => x.GetCurrentRelationshipsBySpecificationId(specificationId))
                .ReturnsAsync(new ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>>(HttpStatusCode.OK, relationships));
        }

        private void GivenNoSpecificationSummaryForSpecificaiton(string specificationId)
        {
            _specificationService.Setup(x => x.GetSpecificationSummaryById(specificationId))
                .ReturnsAsync((SpecificationSummary)null);
        }

        private void AndSpecificationSummaryForSpecificaiton(string specificationId, SpecificationSummary specificationSummary)
        {
            _specificationService.Setup(x => x.GetSpecificationSummaryById(specificationId))
                .ReturnsAsync(specificationSummary);
        }

        private string NewRandomString() => new RandomString();
        private decimal NewRandomDecimal() => decimal.Parse($"{new RandomNumberBetween(0, int.MaxValue)}.{new RandomNumberBetween(0, 99)}");
        private uint NewRandomUint() => (uint)new RandomNumberBetween(1, 99999);
        private int NewRandomInt() => new RandomNumberBetween(1, 99999);

        private Message CreateMessage(string specificationId = null, string relationshipId = null)
        {
            Message message = new Message();
            message.UserProperties.Add("specification-id", specificationId);
            message.UserProperties.Add("relationship-id", relationshipId);

            return message;
        }

        private DatasetSpecificationRelationshipViewModel NewDatasetSpecificationRelationshipViewModel(Action<DatasetSpecificationRelationshipViewModelBuilder> setup = null)
            => BuildNewModel<DatasetSpecificationRelationshipViewModel, DatasetSpecificationRelationshipViewModelBuilder>(setup);

        private PublishedSpecificationConfiguration NewPublishedSpecificationConfiguration(Action<PublishedSpecificationConfigurationBuilder> setup = null)
            => BuildNewModel<PublishedSpecificationConfiguration, PublishedSpecificationConfigurationBuilder>(setup);

        private SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setup = null)
            => BuildNewModel<SpecificationSummary, SpecificationSummaryBuilder>(setup);

        private PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setup = null)
            => BuildNewModel<PublishedProvider, PublishedProviderBuilder>(setup);

        private PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setup = null)
            => BuildNewModel<PublishedProviderVersion, PublishedProviderVersionBuilder>(setup);

        private PublishedSpecificationItem NewPublishedSpecificationItem(Action<PublishedSpecificationItemBuilder> setup = null)
            => BuildNewModel<PublishedSpecificationItem, PublishedSpecificationItemBuilder>(setup);

        private Provider NewProvider(Action<ProviderBuilder> setup = null)
            => BuildNewModel<Provider, ProviderBuilder>(setup);

        private FundingLine NewFundingLine(Action<FundingLineBuilder> setup = null)
            => BuildNewModel<FundingLine, FundingLineBuilder>(setup);

        private FundingCalculation NewFundingCalculation(Action<FundingCalculationBuilder> setup = null)
            => BuildNewModel<FundingCalculation, FundingCalculationBuilder>(setup);

        private T BuildNewModel<T,TB>(Action<TB> setup) where TB:TestEntityBuilder, new()
        {
            dynamic builder = new TB();
            setup?.Invoke(builder);
            return builder.Build();
        }
    }
}
