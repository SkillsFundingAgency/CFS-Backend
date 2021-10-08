using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.DataSets.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Services;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Tests.Common.Builders;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Calcs.UnitTests.Services
{
    [TestClass]
    public class ReferencedSpecificationReMapServiceTests : TemplateMappingTestBase
    {
        private Mock<ICalculationsRepository> _calculationsRepository;
        private Mock<IPoliciesApiClient> _policies;
        private Mock<ISpecificationsApiClient> _specificationApiClient;
        private Mock<IDatasetsApiClient> _datasetsApiClient;
        private Mock<IJobManagement> _jobManagement;
        private Mock<ILogger> _logger;
        private ICalculationCodeReferenceUpdate _calculationCodeReferenceUpdate;
        private ITypeIdentifierGenerator _typeIdentifierGenerator;

        private string _specificationId;
        private string _fundingStreamId;
        private string _datasetSpecificationRelationshipId;
        private string _fundingPeriodId;
        private string _correlationId;
        private string _templateVersion;
        private string _userId;
        private string _userName;
        private string _jobId;
        private Message _message;

        private const string SpecificationId = "specification-id";
        private const string DatasetSpecificationRelationshipId = "dataset-specification-relationship-id";
        private const string CorrelationId = "sfa-correlationId";
        private const string UserId = "user-id";
        private const string UserName = "user-name";
        private const string JobId = "jobId";

        private ReferencedSpecificationReMapService _service;

        [TestInitialize]
        public void SetUp()
        {
            _policies = new Mock<IPoliciesApiClient>();
            _specificationApiClient = new Mock<ISpecificationsApiClient>();
            _datasetsApiClient = new Mock<IDatasetsApiClient>();
            _calculationsRepository = new Mock<ICalculationsRepository>();
            _jobManagement = new Mock<IJobManagement>();
            _logger = new Mock<ILogger>();

            _userId = $"{NewRandomString()}_userId";
            _userName = $"{NewRandomString()}_userName";
            _correlationId = $"{NewRandomString()}_correlationId";
            _specificationId = $"{NewRandomString()}_specificationId";
            _datasetSpecificationRelationshipId = $"{NewRandomString()}_datasetSpecificationRelationshipId";
            _fundingStreamId = $"{NewRandomString()}_fundingStreamId";
            _fundingPeriodId = $"{NewRandomString()}_fundingPeriodId";
            _templateVersion = $"{NewRandomString()}_templateVersion";
            _jobId = $"{NewRandomString()}_jobId";

            _calculationCodeReferenceUpdate = new CalculationCodeReferenceUpdate();
            _typeIdentifierGenerator = new VisualBasicTypeIdentifierGenerator();

            _service = new ReferencedSpecificationReMapService(
                new ResiliencePolicies
                {
                    PoliciesApiClient = Policy.NoOpAsync(),
                    CalculationsRepository = Policy.NoOpAsync(),
                    SpecificationsApiClient = Policy.NoOpAsync(),
                    DatasetsApiClient = Policy.NoOpAsync()
                },
                _specificationApiClient.Object,
                _calculationsRepository.Object,
                _policies.Object,
                _datasetsApiClient.Object,
                _calculationCodeReferenceUpdate,
                _jobManagement.Object,
                _logger.Object);
        }

        [TestMethod]
        public void ThrowsExceptionIfNoMessageSupplied()
        {
            ArgumentNullExceptionShouldBeThrown("message");
        }

        [TestMethod]
        public void ThrowsExceptionIfNoSpecificationIdInMessage()
        {
            GivenTheOtherwiseValidMessage(_ => _.WithoutUserProperty(SpecificationId));

            ArgumentNullExceptionShouldBeThrown(SpecificationId);
        }

        [TestMethod]
        public void ThrowsExceptionIfNoDatasetSpecificationRelationshipIdInMessage()
        {
            GivenTheOtherwiseValidMessage(_ => _.WithoutUserProperty(DatasetSpecificationRelationshipId));

            ArgumentNullExceptionShouldBeThrown(DatasetSpecificationRelationshipId);
        }

        [TestMethod]
        public void ThrowsExceptionIfCantLocateTemplateMappingForTheSuppliedSpecificationIdAndFundingStreamId()
        {
            GivenAValidMessage();
            AndTheSpecificationIsReturned();
            AndTheDatasetSpecificationRelationship();

            ThenAnExceptionShouldBeThrownWithMessage(
                $"Did not locate Template Mapping for funding stream id {_fundingStreamId} and specification id {_specificationId}");
        }

        [TestMethod]
        public void ThrowsExceptionIfCantLocateTemplateContentsForTheSuppliedFundingStreamIdAndTemplateVersion()
        {
            GivenAValidMessage();
            AndTheSpecificationIsReturned();
            AndTheDatasetSpecificationRelationship();
            AndTheTemplateMapping(NewTemplateMapping());

            ThenAnExceptionShouldBeThrownWithMessage(
                $"Did not locate Template Metadata Contents for funding stream id {_fundingStreamId}, funding period id {_fundingPeriodId} and template version {_templateVersion}");
        }

        [TestMethod]
        public async Task ReferencedSpecificationReMapService_WhenFundingLineNameAndCalculationNameChangedCalculationSourceUpdatedAndRelationshipUpdated()
        {
            uint calculationTemplateId = 100;
            string calculationTemplateName = "Calculation name";
            string calculationTemplateSourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(calculationTemplateName);

            uint fundingLineTemplateId = 200;
            string fundingLineTemplateName = "Funding line name";
            string fundingLineTemplateSourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(fundingLineTemplateName);

            GivenAValidMessage();
            AndTheSpecificationIsReturned();
            AndTheDatasetSpecificationRelationship(
                new[]
                {
                    new PublishedSpecificationItem {
                        TemplateId = calculationTemplateId,
                        Name = calculationTemplateName,
                        SourceCodeName = calculationTemplateSourceCodeName
                    }
                },
                new[]
                {
                    new PublishedSpecificationItem {
                        TemplateId = fundingLineTemplateId,
                        Name = fundingLineTemplateName,
                        SourceCodeName = fundingLineTemplateSourceCodeName
                    }
                }
            );

            AndTheTemplateMapping(NewTemplateMapping(_ => _.WithItems(new[] { new TemplateMappingItem {
                TemplateId = calculationTemplateId,
                Name = $"{calculationTemplateName}_updated"
            } })));

            AndTheTemplateMetaDataContents(NewTemplateMetadataContents(_ => _.WithFundingLines(new[] { new FundingLine{
                TemplateLineId = fundingLineTemplateId,
                Name = $"{fundingLineTemplateName}_updated"
            } })));

            AndTheCalculationsExist(new Calculation
            {
                Id = NewRandomString(),
                Current = new CalculationVersion
                {
                    SourceCode = @$"Dim calc as Decimal? = _1619.{calculationTemplateSourceCodeName}()
Dim fundingLine as Decimal? = _1619.FundingLines.{fundingLineTemplateSourceCodeName}()
return calc + fundingLine"
                }
            });

            AndTheDatasetSpecificationRelationshipUpdated();

            await WhenTheReferencedSpecificationReMapped();

            ThenTheCalculationsAreUpdated(new Calculation
            {
                Id = NewRandomString(),
                Current = new CalculationVersion
                {
                    SourceCode = @$"Dim calc as Decimal? = _1619.{calculationTemplateSourceCodeName}_updated()
Dim fundingLine as Decimal? = _1619.FundingLines.{fundingLineTemplateSourceCodeName}_updated()
return calc + fundingLine"
                }
            });
        }

        [TestMethod]
        public async Task ReferencedSpecificationReMapService_WhenQueueReferencedSpecificationReMapJobsForSpecificationRequestedJobsSuccessfullyQueued()
        {
            string jobId = NewRandomString();

            AndJobQueued(JobConstants.DefinitionNames.ReferencedSpecificationReMapJob, jobId);

            IActionResult actionResult = await WhenQueueReferencedSpecificationReMapJob();

            actionResult
                .Should()
                .BeAssignableTo<OkObjectResult>()
                .And
                .NotBeNull();

            OkObjectResult okObjectResult = actionResult as OkObjectResult;

            okObjectResult.Value.Should().NotBeNull().And.BeAssignableTo<Job>();

            Job actualJob = okObjectResult.Value as Job;

            actualJob.Id.Should().Be(jobId);
            actualJob.SpecificationId.Should().Be(_specificationId);
            actualJob.Properties[DatasetSpecificationRelationshipId].Should().Be(_datasetSpecificationRelationshipId);
        }

        private void AndTheDatasetSpecificationRelationshipUpdated()
        {
            _datasetsApiClient.Setup(_ => _.UpdateDefinitionSpecificationRelationship(It.IsAny<UpdateDefinitionSpecificationRelationshipModel>(),
                _specificationId,
                _datasetSpecificationRelationshipId))
            .ReturnsAsync(new ValidatedApiResponse<DefinitionSpecificationRelationshipVersion>(HttpStatusCode.OK, new DefinitionSpecificationRelationshipVersion()));
        }

        private void AndTheDatasetSpecificationRelationship(PublishedSpecificationItem[] calculations = null, PublishedSpecificationItem[] fundingLines = null)
        {
            _datasetsApiClient.Setup(_ => _.GetReferenceRelationshipsBySpecificationId(_specificationId))
                .ReturnsAsync(new ApiResponse<IEnumerable<DatasetSpecificationRelationshipViewModel>>
                (
                    HttpStatusCode.OK,
                    new[] { new DatasetSpecificationRelationshipViewModel
                        {
                            Id = _datasetSpecificationRelationshipId,
                            PublishedSpecificationConfiguration = new PublishedSpecificationConfiguration {
                                Calculations = calculations,
                                FundingLines = fundingLines
                            }
                        }
                    }
                ));
        }

        private void AndTheCalculationsExist(Calculation calculation)
        {
            _calculationsRepository.Setup(_ => _.GetCalculationsBySpecificationId(_specificationId))
                .ReturnsAsync(new[] { calculation });
        }

        private void AndJobQueued(string jobDefinition, string jobId)
        {
            _jobManagement.Setup(_ => _.QueueJob(It.Is<JobCreateModel>(job => job.JobDefinitionId == jobDefinition && job.SpecificationId == _specificationId && job.Properties["dataset-specification-relationship-id"] == _datasetSpecificationRelationshipId)))
                .ReturnsAsync(new Job
                {
                    JobDefinitionId = jobDefinition,
                    SpecificationId = _specificationId,
                    Properties = new Dictionary<string, string> { { DatasetSpecificationRelationshipId, _datasetSpecificationRelationshipId } },
                    Id = jobId
                });
        }

        private void ThenTheCalculationsAreUpdated(Calculation calculation)
        {
            _calculationsRepository.Verify(_ => _.UpdateCalculations(It.Is<IEnumerable<Calculation>>(_ =>
                _.All(calc => calc.Current.SourceCode == calculation.Current.SourceCode))
            ), Times.Once);
        }

        private void AndTheSpecificationIsReturned()
        {
            _specificationApiClient.Setup(_ => _.GetSpecificationSummaryById(_specificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>
                (
                    HttpStatusCode.OK,
                    new SpecificationSummary
                    {
                        Id = _specificationId,
                        FundingStreams = new[] { new Reference(_fundingStreamId, "") },
                        FundingPeriod = new Reference(_fundingPeriodId, ""),
                        TemplateIds = new Dictionary<string, string> { { _fundingStreamId, _templateVersion } }
                    }
                ));
        }

        private void ThenAnExceptionShouldBeThrownWithMessage(string expectedMessage)
        {
            Func<Task> invocation = WhenTheReferencedSpecificationReMapped;

            invocation
                .Should().Throw<Exception>()
                .WithMessage(expectedMessage);
        }

        private void ArgumentNullExceptionShouldBeThrown(string parameterName)
        {
            Func<Task> invocation = WhenTheReferencedSpecificationReMapped;

            invocation
                .Should().Throw<ArgumentNullException>()
                .And.ParamName
                .Should().Be(parameterName);
        }

        private void GivenAValidMessage()
        {
            GivenTheOtherwiseValidMessage();
        }

        private void GivenTheOtherwiseValidMessage(Action<MessageBuilder> overrides = null)
        {
            MessageBuilder messageBuilder = new MessageBuilder()
                .WithUserProperty(SpecificationId, _specificationId)
                .WithUserProperty(CorrelationId, _correlationId)
                .WithUserProperty(DatasetSpecificationRelationshipId, _datasetSpecificationRelationshipId)
                .WithUserProperty(UserId, _userId)
                .WithUserProperty(UserName, _userName)
                .WithUserProperty(JobId, _jobId);

            overrides?.Invoke(messageBuilder);

            _message = messageBuilder.Build();
        }

        private void AndTheTemplateMapping(TemplateMapping templateMapping)
        {
            _calculationsRepository.Setup(_ => _.GetTemplateMapping(_specificationId, _fundingStreamId))
                .ReturnsAsync(templateMapping);
        }

        private void AndTheTemplateMetaDataContents(TemplateMetadataContents templateMetadataContents)
        {
            _policies.Setup(_ => _.GetFundingTemplateContents(_fundingStreamId, _fundingPeriodId, _templateVersion, null))
                .ReturnsAsync(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, templateMetadataContents));
        }

        private async Task WhenTheReferencedSpecificationReMapped()
        {
            await _service.Run(_message);
        }

        private async Task<IActionResult> WhenQueueReferencedSpecificationReMapJob()
        {
            return await _service.QueueReferencedSpecificationReMapJob(_specificationId,
                _datasetSpecificationRelationshipId,
                new Reference(_userId, _userName),
                _correlationId);
        }
    }
}