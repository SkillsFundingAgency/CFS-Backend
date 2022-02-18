using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Linq;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using Microsoft.FeatureManagement;

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
        private readonly Mock<IReleaseManagementRepository> _releaseManagementRepository;
        private readonly Mock<IFeatureManagerSnapshot> _featureManagerSnapshot;
        private readonly Mock<IFundingConfigurationService> _fundingConfigurationService;

        private readonly string _fundingStreamId;
        private readonly string _channelCode;
        private readonly string _specificationId;
        private readonly int _channelId;
        private readonly int _majorVersion = 1;

        private readonly IDatasetsDataCopyService _service;

        public DatasetsDataCopyServiceTests()
        {
            _jobManagement = new Mock<IJobManagement>();
            _logger = new Mock<ILogger>();
            _datasetsApiClient = new Mock<IDatasetsApiClient>();
            _specificationService = new Mock<ISpecificationService>();
            _publishedFundingRepository = new Mock<IPublishedFundingRepository>();
            _releaseManagementRepository = new Mock<IReleaseManagementRepository>();
            _featureManagerSnapshot = new Mock<IFeatureManagerSnapshot>();
            _fundingConfigurationService = new Mock<IFundingConfigurationService>();

            _specificationId = NewRandomString();
            _fundingStreamId = NewRandomString();
            _channelCode = NewRandomString();
            _channelId = NewRandomInt();

            IPublishingResiliencePolicies policies = PublishingResilienceTestHelper.GenerateTestPolicies();

            _service = new DatasetsDataCopyService(
                                        _jobManagement.Object,
                                        _logger.Object,
                                        _datasetsApiClient.Object,
                                        _specificationService.Object,
                                        policies,
                                        _publishedFundingRepository.Object,
                                        _releaseManagementRepository.Object,
                                        _featureManagerSnapshot.Object,
                                        _fundingConfigurationService.Object);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Process_GivenNoSpecificationIdOnMessage_ThrowsException(bool isReleaseManagementEnabled)
        {
            // Arrange
            GivenFeatureEnabled(isReleaseManagementEnabled);
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

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Process_GivenNoSpecificationFound_ThrowsException(bool isReleaseManagementEnabled)
        {
            // Arrange
            string relationshipId = NewRandomString();
            Message message = CreateMessage(_specificationId, relationshipId);

            GivenFeatureEnabled(isReleaseManagementEnabled);
            GivenNoSpecificationSummaryForSpecification(_specificationId);

            // Act
            Func<Task> result = async () => await _service.Process(message);

            // Assert
            result
                .Should()
                .Throw<NonRetriableException>()
                .Which
                .Message
                .Should()
                .Be($"Specification not found for specification id- {_specificationId}");

            _specificationService.Verify(x => x.GetSpecificationSummaryById(_specificationId), Times.Once);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Process_GivenDatasetFileIsFailedToUpload_ThrowsException(bool isReleaseManagementEnabled)
        {
            // Arrange
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

            Message message = CreateMessage(_specificationId, relationshipId);

            GivenFeatureEnabled(isReleaseManagementEnabled);

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

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithFundingStreamIds(_fundingStreamId)
                .WithId(_specificationId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithTemplateIds((fundingPeriodId, templateId)));

            IEnumerable<PublishedProvider> publishedProviders = new[]
            {
                NewPublishedProvider(pp => pp.WithReleased(
                    NewPublishedProviderVersion(_ => _.WithProvider(NewProvider(p => p.WithUKPRN(Ukprn1)))
                        .WithMajorVersion(_majorVersion)
                        .WithMinorVersion(0)
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
                FundingStreamId = _fundingStreamId
            };

            byte[] excelData = new byte[0];

            GivenRelationshipsForSpecification(_specificationId, relationships);
            AndSpecificationSummaryForSpecificaiton(_specificationId, specificationSummary);
            AndPublishProvidersForSpecification(_specificationId, publishedProviders);
            AndLatestPublishedProviderVersions(publishedProviders.Select(_ => new ProviderVersionInChannel
            {
                ChannelCode = _channelCode,
                ChannelId = _channelId,
                ChannelName = NewRandomString(),
                CoreProviderVersionId = _.Released.PublishedProviderId,
                MajorVersion = _majorVersion,
                MinorVersion = 0,
                ProviderId = _.Released.ProviderId
            }));
            AndReleasedProviderVersions(publishedProviders.Select(_ => _.Released));
            GivenDatasetVersionUpdate(datasetId, relationshipName, _fundingStreamId, datasetVersionResponse);
            GivenFailedToUploadOfDatasetFile(datasetVersionResponse, HttpStatusCode.BadRequest);

            // Act
            Func<Task> result = async () => await _service.Process(message);

            // Assert
            result
               .Should()
               .ThrowAsync<Exception>()
               .Result
               .Which
               .Message
               .Should()
               .Be($"Failed to upload the dataset file, dataset id - {datasetId}, file name - {fileName}. Status Code - {HttpStatusCode.BadRequest}");

            AssertRelationshipsRetrievedBySpecificationId(_specificationId);
            AssertSpecificationSummaryRetrievedBySpecificationId(_specificationId);
            AssertPublishedProvidersForSpecificationRetrievedForBatchProcessing(_specificationId, isReleaseManagementEnabled, publishedProviders.Count());
            AssertDatasetVersionUpdateHasBeenCalled(_fundingStreamId, relationshipName, datasetId);
            AssertDatasetFileUpload(datasetVersionResponse);
            AssertThatRelationshipUpdatedNotCalled(relationshipId, datasetId, datasetVersionVersion);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task Process_GivenValidProvidersData_ShouldCreateNewDatasetVersionAndUpdateTheRelationshipWithNewDatasetVersion(bool isReleaseManagementEnabled)
        {
            // Arrange
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

            GivenFeatureEnabled(isReleaseManagementEnabled);
            Message message = CreateMessage(_specificationId, relationshipId);

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

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithFundingStreamIds(_fundingStreamId)
                .WithId(_specificationId)
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
                    Calculations = new Dictionary<string, object>{ { $"Calc_{calculationTemplateId1}_{calculation1}", null }, { $"Calc_{calculationTemplateId2}_{calculation2}", calculationValue2 } }
                },
                new RelationshipDataSetExcelData(Ukprn2)
                {
                    FundingLines = new Dictionary<string, decimal?> { { $"FL_{fundingLineTemplateId1}_{fundingLine1}", null }, { $"FL_{fundingLineTemplateId2}_{fundingLine2}", fundingLineValue2 } },
                    Calculations = new Dictionary<string, object>{ { $"Calc_{calculationTemplateId1}_{calculation1}", calculationValue1 }, { $"Calc_{calculationTemplateId2}_{calculation2}", null } }
                },
                new RelationshipDataSetExcelData(Ukprn3)
                {
                    FundingLines = new Dictionary<string, decimal?> { { $"FL_{fundingLineTemplateId1}_{fundingLine1}", fundingLineValue1 }, { $"FL_{fundingLineTemplateId2}_{fundingLine2}", null } },
                    Calculations = new Dictionary<string, object>{ { $"Calc_{calculationTemplateId1}_{calculation1}", calculationValue1 }, { $"Calc_{calculationTemplateId2}_{calculation2}", calculationValue2 } }
                }
            };

            NewDatasetVersionResponseModel datasetVersionResponse = new NewDatasetVersionResponseModel()
            {
                DatasetId = datasetId,
                Version = datasetVersionVersion,
                Filename = fileName,
                FundingStreamId = _fundingStreamId
            };

            byte[] excelData = new byte[0];

            GivenRelationshipsForSpecification(_specificationId, relationships);
            AndSpecificationSummaryForSpecificaiton(_specificationId, specificationSummary);
            AndPublishProvidersForSpecification(_specificationId, publishedProviders);
            AndLatestPublishedProviderVersions(publishedProviders.Select(_ => new ProviderVersionInChannel
            {
                ChannelCode = _channelCode,
                ChannelId = _channelId,
                ChannelName = NewRandomString(),
                CoreProviderVersionId = _.Released.PublishedProviderId,
                MajorVersion = _majorVersion,
                MinorVersion = 0,
                ProviderId = _.Released.ProviderId
            }));
            AndReleasedProviderVersions(publishedProviders.Select(_ => _.Released));
            GivenDatasetVersionUpdate(datasetId, relationshipName, _fundingStreamId, datasetVersionResponse);
            AndSuccessfulUploadOfDatasetFile(datasetVersionResponse);
            AndAssignDatasetVersionToRelationship(datasetId, relationshipId, datasetVersionVersion);

            // Act
            await _service.Process(message);

            // Assert
            AssertRelationshipsRetrievedBySpecificationId(_specificationId);
            AssertSpecificationSummaryRetrievedBySpecificationId(_specificationId);
            AssertPublishedProvidersForSpecificationRetrievedForBatchProcessing(_specificationId, isReleaseManagementEnabled, publishedProviders.Count());
            AssertDatasetVersionUpdateHasBeenCalled(_fundingStreamId, relationshipName, datasetId);
            AssertDatasetFileUpload(datasetVersionResponse);
            AssertThatRelationshipUpdatedWithNewDatasetVersion(relationshipId, datasetId, datasetVersionVersion);
        }

        [DataTestMethod]
        [DataRow(true, false)]
        [DataRow(false, false)]
        public async Task Process_GivenValidProvidersData_AndNoDatasetExists_ShouldCreateNewDatasetVersionAndUpdateTheRelationshipWithNewDatasetVersion_And_CreateDataset(
            bool hasTargetSpecificationId, bool isReleaseManagementEnabled)
        {
            // Arrange
            string fundingPeriodId = NewRandomString();
            string templateId = NewRandomString();
            string relationshipId = NewRandomString();
            string relationshipName = NewRandomString().Substring(0, 30); // Used for excel worksheet name
            string targetSpecificationId = hasTargetSpecificationId ? NewRandomString() : null;

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

            GivenFeatureEnabled(isReleaseManagementEnabled);

            Message message = CreateMessage(_specificationId, relationshipId);

            IEnumerable<DatasetSpecificationRelationshipViewModel> relationships = new[]
            {
                NewDatasetSpecificationRelationshipViewModel(_ =>
                _.WithId(relationshipId)
                 .WithName(relationshipName)
                 .WithTargetSpecificationId(targetSpecificationId)
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

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithFundingStreamIds(_fundingStreamId)
                .WithId(_specificationId)
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
                    Calculations = new Dictionary<string, object>{ { $"Calc_{calculationTemplateId1}_{calculation1}", null }, { $"Calc_{calculationTemplateId2}_{calculation2}", calculationValue2 } }
                },
                new RelationshipDataSetExcelData(Ukprn2)
                {
                    FundingLines = new Dictionary<string, decimal?> { { $"FL_{fundingLineTemplateId1}_{fundingLine1}", null }, { $"FL_{fundingLineTemplateId2}_{fundingLine2}", fundingLineValue2 } },
                    Calculations = new Dictionary<string, object>{ { $"Calc_{calculationTemplateId1}_{calculation1}", calculationValue1 }, { $"Calc_{calculationTemplateId2}_{calculation2}", null } }
                },
                new RelationshipDataSetExcelData(Ukprn3)
                {
                    FundingLines = new Dictionary<string, decimal?> { { $"FL_{fundingLineTemplateId1}_{fundingLine1}", fundingLineValue1 }, { $"FL_{fundingLineTemplateId2}_{fundingLine2}", null } },
                    Calculations = new Dictionary<string, object>{ { $"Calc_{calculationTemplateId1}_{calculation1}", calculationValue1 }, { $"Calc_{calculationTemplateId2}_{calculation2}", calculationValue2 } }
                }
            };

            NewDatasetVersionResponseModel datasetVersionResponse = new NewDatasetVersionResponseModel()
            {
                DatasetId = datasetId,
                Version = datasetVersionVersion,
                Filename = fileName,
                FundingStreamId = _fundingStreamId
            };

            byte[] excelData = new byte[0];

            GivenRelationshipsForSpecification(_specificationId, relationships);
            AndSpecificationSummaryForSpecificaiton(_specificationId, specificationSummary);
            AndPublishProvidersForSpecification(_specificationId, publishedProviders);
            AndSuccessfulUploadOfDatasetFile(datasetVersionResponse);
            AndAssignDatasetVersionToRelationship(datasetId, relationshipId, datasetVersionVersion);
            GivenDatasetCreateAndPersistNewDataset(relationshipName, targetSpecificationId ?? _specificationId, datasetVersionResponse);

            // Act
            await _service.Process(message);

            // Assert
            AssertRelationshipsRetrievedBySpecificationId(_specificationId);
            AssertSpecificationSummaryRetrievedBySpecificationId(_specificationId);
            AssertPublishedProvidersForSpecificationRetrievedForBatchProcessing(_specificationId, isReleaseManagementEnabled, publishedProviders.Count());
            AssertCreateAndPersistNewDatasetHasBeenCalled(relationshipName, targetSpecificationId ?? _specificationId);
            AssertDatasetFileUpload(datasetVersionResponse);
            AssertThatRelationshipUpdatedWithNewDatasetVersion(relationshipId, datasetId, datasetVersionVersion);
        }

        [TestMethod]
        public async Task Process_GivenPublishedProviderVersionNotFound_ThrowsException()
        {
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

            GivenFeatureEnabled(true);
            Message message = CreateMessage(_specificationId, relationshipId);

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

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithFundingStreamIds(_fundingStreamId)
                .WithId(_specificationId)
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
                    Calculations = new Dictionary<string, object>{ { $"Calc_{calculationTemplateId1}_{calculation1}", null }, { $"Calc_{calculationTemplateId2}_{calculation2}", calculationValue2 } }
                },
                new RelationshipDataSetExcelData(Ukprn2)
                {
                    FundingLines = new Dictionary<string, decimal?> { { $"FL_{fundingLineTemplateId1}_{fundingLine1}", null }, { $"FL_{fundingLineTemplateId2}_{fundingLine2}", fundingLineValue2 } },
                    Calculations = new Dictionary<string, object>{ { $"Calc_{calculationTemplateId1}_{calculation1}", calculationValue1 }, { $"Calc_{calculationTemplateId2}_{calculation2}", null } }
                },
                new RelationshipDataSetExcelData(Ukprn3)
                {
                    FundingLines = new Dictionary<string, decimal?> { { $"FL_{fundingLineTemplateId1}_{fundingLine1}", fundingLineValue1 }, { $"FL_{fundingLineTemplateId2}_{fundingLine2}", null } },
                    Calculations = new Dictionary<string, object>{ { $"Calc_{calculationTemplateId1}_{calculation1}", calculationValue1 }, { $"Calc_{calculationTemplateId2}_{calculation2}", calculationValue2 } }
                }
            };

            NewDatasetVersionResponseModel datasetVersionResponse = new NewDatasetVersionResponseModel()
            {
                DatasetId = datasetId,
                Version = datasetVersionVersion,
                Filename = fileName,
                FundingStreamId = _fundingStreamId
            };

            byte[] excelData = new byte[0];

            GivenRelationshipsForSpecification(_specificationId, relationships);
            AndSpecificationSummaryForSpecificaiton(_specificationId, specificationSummary);
            AndPublishProvidersForSpecification(_specificationId, publishedProviders);
            AndLatestPublishedProviderVersions(publishedProviders.Select(_ => new ProviderVersionInChannel
            {
                ChannelCode = _channelCode,
                ChannelId = _channelId,
                ChannelName = NewRandomString(),
                CoreProviderVersionId = _.Released.PublishedProviderId,
                MajorVersion = _majorVersion,
                MinorVersion = 0,
                ProviderId = _.Released.ProviderId
            }));
            AndReleasedProviderVersions(publishedProviders.Select(_ => _.Released).Skip(1));
            GivenDatasetVersionUpdate(datasetId, relationshipName, _fundingStreamId, datasetVersionResponse);
            AndSuccessfulUploadOfDatasetFile(datasetVersionResponse);
            AndAssignDatasetVersionToRelationship(datasetId, relationshipId, datasetVersionVersion);

            Func<Task> result = async () => await _service.Process(message);

            result
                .Should()
                .Throw<InvalidOperationException>();
        }

        [TestMethod]
        public async Task Process_GivenChannelCodeNotFound_ThrowsException()
        {
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

            GivenFeatureEnabled(true);
            Message message = CreateMessage(_specificationId, relationshipId);

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

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _.WithFundingStreamIds(_fundingStreamId)
                .WithId(_specificationId)
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
                    Calculations = new Dictionary<string, object>{ { $"Calc_{calculationTemplateId1}_{calculation1}", null }, { $"Calc_{calculationTemplateId2}_{calculation2}", calculationValue2 } }
                },
                new RelationshipDataSetExcelData(Ukprn2)
                {
                    FundingLines = new Dictionary<string, decimal?> { { $"FL_{fundingLineTemplateId1}_{fundingLine1}", null }, { $"FL_{fundingLineTemplateId2}_{fundingLine2}", fundingLineValue2 } },
                    Calculations = new Dictionary<string, object>{ { $"Calc_{calculationTemplateId1}_{calculation1}", calculationValue1 }, { $"Calc_{calculationTemplateId2}_{calculation2}", null } }
                },
                new RelationshipDataSetExcelData(Ukprn3)
                {
                    FundingLines = new Dictionary<string, decimal?> { { $"FL_{fundingLineTemplateId1}_{fundingLine1}", fundingLineValue1 }, { $"FL_{fundingLineTemplateId2}_{fundingLine2}", null } },
                    Calculations = new Dictionary<string, object>{ { $"Calc_{calculationTemplateId1}_{calculation1}", calculationValue1 }, { $"Calc_{calculationTemplateId2}_{calculation2}", calculationValue2 } }
                }
            };

            NewDatasetVersionResponseModel datasetVersionResponse = new NewDatasetVersionResponseModel()
            {
                DatasetId = datasetId,
                Version = datasetVersionVersion,
                Filename = fileName,
                FundingStreamId = _fundingStreamId
            };

            byte[] excelData = new byte[0];

            GivenRelationshipsForSpecification(_specificationId, relationships);
            AndSpecificationSummaryForSpecificaiton(_specificationId, specificationSummary);
            AndPublishProvidersForSpecification(_specificationId, publishedProviders);
            AndLatestPublishedProviderVersions(publishedProviders.Select(_ => new ProviderVersionInChannel
            {
                ChannelCode = _channelCode,
                ChannelId = _channelId,
                ChannelName = NewRandomString(),
                CoreProviderVersionId = _.Released.PublishedProviderId,
                MajorVersion = _majorVersion,
                MinorVersion = 0,
                ProviderId = _.Released.ProviderId
            }), missingChannelCode: true);
            AndReleasedProviderVersions(publishedProviders.Select(_ => _.Released));
            GivenDatasetVersionUpdate(datasetId, relationshipName, _fundingStreamId, datasetVersionResponse);
            AndSuccessfulUploadOfDatasetFile(datasetVersionResponse);
            AndAssignDatasetVersionToRelationship(datasetId, relationshipId, datasetVersionVersion);

            Func<Task> result = async () => await _service.Process(message);

            result
                .Should()
                .Throw<InvalidOperationException>();
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

        private void AssertDatasetVersionUpdateHasBeenCalled(string fundingStreamId, string relationshipName, string datasetId)
        {
            _datasetsApiClient.Verify(x => x.DatasetVersionUpdateAndPersist(
                             It.Is<DatasetVersionUpdateModel>(x => x.DatasetId == datasetId && x.Filename == $"{relationshipName}.xlsx" && x.FundingStreamId == fundingStreamId)), Times.Once);
        }

        private void AssertCreateAndPersistNewDatasetHasBeenCalled(string relationshipName, string specificationId)
        {
            _datasetsApiClient.Verify(x => x.CreateAndPersistNewDataset(It.Is<CreateNewDatasetModel>(c => c.Name == $"{relationshipName}-{specificationId}")), Times.Once);
        }

        private void AssertPublishedProvidersForSpecificationRetrievedForBatchProcessing(string specificationId, bool isReleaseManagementEnabled, int expectedCount)
        {
            if (!isReleaseManagementEnabled)
            {
                _publishedFundingRepository.Verify(x => x.PublishedProviderBatchProcessing(
                        "IS_NULL(c.content.released) = false",
                        specificationId,
                        It.IsAny<Func<List<PublishedProvider>, Task>>(),
                        100,
                        null,
                        null)
                    , Times.Once);
            }
            else
            {
                _publishedFundingRepository.Verify(x => x.GetReleasedPublishedProviderVersionByMajorVersion(
                        _fundingStreamId,
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        specificationId,
                        _majorVersion
                        )
                    , Times.Exactly(expectedCount));
            }
        }

        private void AssertSpecificationSummaryRetrievedBySpecificationId(string specificationId)
        {
            _specificationService.Verify(x => x.GetSpecificationSummaryById(specificationId), Times.Once);
        }

        private void AssertRelationshipsRetrievedBySpecificationId(string specificationId)
        {
            _datasetsApiClient.Verify(x => x.GetReferenceRelationshipsBySpecificationId(specificationId), Times.Once);
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

            foreach (KeyValuePair<string, object> actualCalculation in actual.Calculations)
            {
                if (expected.Calculations.TryGetValue(actualCalculation.Key, out object expectedValue) && actualCalculation.Value == expectedValue) continue;
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

        private void GivenDatasetVersionUpdate(string datasetId, string relationshipName, string fundingStreamId, NewDatasetVersionResponseModel datasetVersionResponse)
        {
            _datasetsApiClient.Setup(x => x.DatasetVersionUpdateAndPersist(It.Is<DatasetVersionUpdateModel>(x => x.DatasetId == datasetId && x.Filename == $"{relationshipName}.xlsx" && x.FundingStreamId == fundingStreamId)))
                 .ReturnsAsync(new ValidatedApiResponse<NewDatasetVersionResponseModel>(HttpStatusCode.OK, datasetVersionResponse));
        }

        private void GivenDatasetCreateAndPersistNewDataset(string relationshipName, string specificationId, NewDatasetVersionResponseModel response)
        {
            _datasetsApiClient.Setup(x => x.CreateAndPersistNewDataset(It.Is<CreateNewDatasetModel>(c => c.Name == $"{relationshipName}-{specificationId}")))
                .ReturnsAsync(new ValidatedApiResponse<NewDatasetVersionResponseModel>(HttpStatusCode.OK, response));
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
            while (count > 0 && queue.Count > 0)
            {
                yield return queue.Dequeue();
                count--;
            }
        }

        private void GivenRelationshipsForSpecification(string specificationId, IEnumerable<DatasetSpecificationRelationshipViewModel> relationships)
        {
            _datasetsApiClient.Setup(x => x.GetReferenceRelationshipsBySpecificationId(specificationId))
                .ReturnsAsync(new ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>>(HttpStatusCode.OK, relationships));
        }

        private void GivenNoSpecificationSummaryForSpecification(string specificationId)
        {
            _specificationService.Setup(x => x.GetSpecificationSummaryById(specificationId))
                .ReturnsAsync((SpecificationSummary)null);
        }

        private void AndReleasedProviderVersions(IEnumerable<PublishedProviderVersion> providers)
        {
            _publishedFundingRepository.Setup(_ => _.GetReleasedPublishedProviderVersionByMajorVersion(
                It.Is<string>(s => s.Equals(_fundingStreamId)),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<string>(s => s.Equals(_specificationId)),
                It.IsAny<int>()
            )).ReturnsAsync(
                (string fundingStreamId, string fundingPeriodId, string providerId, string specId, int majorVersion) =>
                    providers.SingleOrDefault(s => s.ProviderId == providerId));
        }

        private void AndLatestPublishedProviderVersions(IEnumerable<ProviderVersionInChannel> providers, bool missingChannelCode = false)
        {
            _fundingConfigurationService.Setup(_ => _.GetFundingConfigurations(It.IsAny<SpecificationSummary>()))
                .ReturnsAsync(
                    new Dictionary<string, FundingConfiguration>
                    {
                        { _fundingStreamId, new FundingConfiguration { SpecToSpecChannelCode = missingChannelCode ? "" : _channelCode } }
                    });

            _releaseManagementRepository
                .Setup(_ => _.GetChannelByChannelCode(It.Is<string>(s => s.Equals(_channelCode))))
                .ReturnsAsync(new Channel { ChannelId = _channelId });

            _releaseManagementRepository.Setup(_ =>
                    _.GetLatestPublishedProviderVersions(It.Is<string>(s => s.Equals(_specificationId)),
                        It.IsAny<IEnumerable<int>>()))
                .ReturnsAsync(providers);
        }

        private void GivenFeatureEnabled(bool enabled)
        {
            _featureManagerSnapshot.Setup(_ =>
                    _.IsEnabledAsync(It.Is<string>(s => s.Equals("EnableReleaseManagementBackend"))))
                .ReturnsAsync(enabled);
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

        private T BuildNewModel<T, TB>(Action<TB> setup) where TB : TestEntityBuilder, new()
        {
            dynamic builder = new TB();
            setup?.Invoke(builder);
            return builder.Build();
        }
    }
}
