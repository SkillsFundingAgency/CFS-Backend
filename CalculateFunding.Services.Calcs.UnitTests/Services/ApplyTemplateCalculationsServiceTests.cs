using System;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Calcs.Services
{
    [TestClass]
    public class ApplyTemplateCalculationsServiceTests : TemplateMappingTestBase
    {
        private ICreateCalculationService _createCalculationService;
        private ICalculationsRepository _calculationsRepository;
        private ITemplateContentsCalculationQuery _calculationQuery;
        private IApplyTemplateCalculationsJobTrackerFactory _jobTrackerFactory;
        private IApplyTemplateCalculationsJobTracker _jobTracker;
        private IInstructionAllocationJobCreation _instructionAllocationJobCreation;
        private IPoliciesApiClient _policies;

        private string _specificationId;
        private string _fundingStreamId;
        private string _correlationId;
        private string _templateVersion;
        private string _userId;
        private string _userName;
        private Message _message;

        private const string SpecificationId = "specification-id";
        private const string FundingStreamId = "fundingstream-id";
        private const string TemplateVersion = "template-version";
        private const string CorrelationId = "sfa-correlationId";
        private const string UserId = "user-id";
        private const string UserName = "user-name";

        private ApplyTemplateCalculationsService _service;

        [TestInitialize]
        public void SetUp()
        {
            _policies = Substitute.For<IPoliciesApiClient>();
            _createCalculationService = Substitute.For<ICreateCalculationService>();
            _calculationQuery = Substitute.For<ITemplateContentsCalculationQuery>();
            _calculationsRepository = Substitute.For<ICalculationsRepository>();
            _jobTrackerFactory = Substitute.For<IApplyTemplateCalculationsJobTrackerFactory>();
            _jobTracker = Substitute.For<IApplyTemplateCalculationsJobTracker>();
            _instructionAllocationJobCreation = Substitute.For<IInstructionAllocationJobCreation>();

            _jobTrackerFactory.CreateJobTracker(Arg.Any<Message>())
                .Returns(_jobTracker);

            _jobTracker.NotifyProgress(Arg.Any<int>())
                .Returns(Task.CompletedTask);
            _jobTracker.TryStartTrackingJob()
                .Returns(Task.FromResult(true));
            _jobTracker.CompleteTrackingJob(Arg.Any<string>(), Arg.Any<int>())
                .Returns(Task.CompletedTask);

            _userId = $"{NewRandomString()}_userId";
            _userName = $"{NewRandomString()}_userName";
            _correlationId = $"{NewRandomString()}_correlationId";
            _specificationId = $"{NewRandomString()}_specificationId";
            _fundingStreamId = $"{NewRandomString()}_fundingStreamId";
            _templateVersion = $"{NewRandomString()}_templateVersion";

            _calculationsRepository.UpdateTemplateMapping(Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<TemplateMapping>())
                .Returns(Task.CompletedTask);

            _service = new ApplyTemplateCalculationsService(_createCalculationService,
                _policies,
                new ResiliencePolicies
                {
                    PoliciesApiClient = Policy.NoOpAsync(),
                    CalculationsRepository = Policy.NoOpAsync()
                },
                _calculationsRepository,
                _calculationQuery,
                _jobTrackerFactory,
                _instructionAllocationJobCreation,
                Substitute.For<ILogger>());
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
        public void ThrowsExceptionIfNoFundingStreamIdInMessage()
        {
            GivenTheOtherwiseValidMessage(_ => _.WithoutUserProperty(FundingStreamId));

            ArgumentNullExceptionShouldBeThrown(FundingStreamId);
        }

        [TestMethod]
        public void ThrowsExceptionIfNoTemplateVersionInMessage()
        {
            GivenTheOtherwiseValidMessage(_ => _.WithoutUserProperty(TemplateVersion));

            ArgumentNullExceptionShouldBeThrown(TemplateVersion);
        }

        [TestMethod]
        public void ThrowsExceptionIfCantLocateTemplateMappingForTheSuppliedSpecificationIdAndFundingStreamId()
        {
            GivenAValidMessage();

            ThenAnExceptionShouldBeThrownWithMessage(
                $"Did not locate Template Mapping for funding stream id {_fundingStreamId} and specification id {_specificationId}");
        }

        [TestMethod]
        public void ThrowsExceptionIfCantLocateTemplateContentsForTheSuppliedFundingStreamIdAndTemplateVersion()
        {
            GivenAValidMessage();
            AndTheTemplateMapping(NewTemplateMapping());

            ThenAnExceptionShouldBeThrownWithMessage(
                $"Did not locate Template Metadata Contents for funding stream id {_fundingStreamId} and template version {_templateVersion}");
        }

        [TestMethod]
        public void ThrowsExceptionIfCreateCallFailsWhenCreatingMissingCalculations()
        {
            TemplateMappingItem mappingWithMissingCalculation1 = NewTemplateMappingItem();
            TemplateMapping templateMapping = NewTemplateMapping(_ => _.WithItems(mappingWithMissingCalculation1));
            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents();
            TemplateCalculation templateCalculationOne = NewTemplateMappingCalculation(_ => _.WithName("template calculation 1"));

            GivenAValidMessage();
            AndTheTemplateMapping(templateMapping);
            AndTheTemplateMetaDataContents(templateMetadataContents);
            AndTheTemplateContentsCalculation(mappingWithMissingCalculation1, templateMetadataContents, templateCalculationOne);

            ThenAnExceptionShouldBeThrownWithMessage("Unable to create new default template calculation for template mapping");
        }

        [TestMethod]
        public async Task CreatesCalculationsIfOnTemplateMappingButDontExistYet()
        {
            TemplateMappingItem mappingWithMissingCalculation1 = NewTemplateMappingItem();
            TemplateMappingItem mappingWithMissingCalculation2 = NewTemplateMappingItem();

            TemplateMapping templateMapping = NewTemplateMapping(_ => _.WithItems(mappingWithMissingCalculation1,
                NewTemplateMappingItem(mi => mi.WithCalculationId(NewRandomString())),
                mappingWithMissingCalculation2,
                NewTemplateMappingItem(mi => mi.WithCalculationId(NewRandomString()))));

            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(NewFundingLine(fl =>
                fl.WithCalculations(
                    NewTemplateMappingCalculation(),
                    NewTemplateMappingCalculation(),
                    NewTemplateMappingCalculation()))));
            TemplateCalculation templateCalculationOne = NewTemplateMappingCalculation(_ => _.WithName("template calculation 1"));
            TemplateCalculation templateCalculationTwo = NewTemplateMappingCalculation(_ => _.WithName("template calculation 2"));

            string newCalculationId1 = NewRandomString();
            string newCalculationId2 = NewRandomString();

            GivenAValidMessage();
            AndTheTemplateMapping(templateMapping);
            AndTheTemplateMetaDataContents(templateMetadataContents);
            AndTheCalculationIsCreatedForRequestMatching(_ => _.Name == templateCalculationOne.Name &&
                                                              _.SourceCode == "return 0" &&
                                                              _.SpecificationId == _specificationId &&
                                                              _.FundingStreamId == _fundingStreamId &&
                                                              _.ValueType.GetValueOrDefault()
                                                              == templateCalculationOne.ValueFormat.AsMatchingEnum<CalculationValueType>(),
                NewCalculation(_ => _.WithId(newCalculationId1)));
            AndTheCalculationIsCreatedForRequestMatching(_ => _.Name == templateCalculationTwo.Name &&
                                                              _.SourceCode == "return 0" &&
                                                              _.SpecificationId == _specificationId &&
                                                              _.FundingStreamId == _fundingStreamId &&
                                                              _.ValueType.GetValueOrDefault()
                                                              == templateCalculationTwo.ValueFormat.AsMatchingEnum<CalculationValueType>(),
                NewCalculation(_ => _.WithId(newCalculationId2)));
            AndTheTemplateContentsCalculation(mappingWithMissingCalculation1, templateMetadataContents, templateCalculationOne);
            AndTheTemplateContentsCalculation(mappingWithMissingCalculation2, templateMetadataContents, templateCalculationTwo);

            await WhenTheTemplateCalculationsAreApplied();

            mappingWithMissingCalculation1
                .CalculationId
                .Should().Be(newCalculationId1);

            mappingWithMissingCalculation2
                .CalculationId
                .Should().Be(newCalculationId2);

            AndTheTemplateMappingWasUpdated(templateMapping);
            AndTheJobsStartWasLogged();
            AndTheProgressNotificationsWereMade(1);
            AndTheJobCompletionWasLogged(3);
            AndACalculationRunWasInitialised();
        }

        private void AndTheCalculationIsCreatedForRequestMatching(Expression<Predicate<CalculationCreateModel>> createModelMatching, Calculation calculation)
        {
            _createCalculationService.CreateCalculation(Arg.Is(_specificationId),
                    Arg.Is(createModelMatching),
                    Arg.Is(CalculationNamespace.Template),
                    Arg.Is(CalculationType.Template),
                    Arg.Is<Reference>(_ => _.Id == _userId &&
                                           _.Name == _userName),
                    Arg.Is(_correlationId),
                    Arg.Is(false))
                .Returns(new CreateCalculationResponse
                {
                    Succeeded = true,
                    Calculation = calculation
                });
        }

        private void ThenAnExceptionShouldBeThrownWithMessage(string expectedMessage)
        {
            Func<Task> invocation = WhenTheTemplateCalculationsAreApplied;

            invocation
                .Should().Throw<Exception>()
                .WithMessage(expectedMessage);
        }

        private void ArgumentNullExceptionShouldBeThrown(string parameterName)
        {
            Func<Task> invocation = WhenTheTemplateCalculationsAreApplied;

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
                .WithUserProperty(FundingStreamId, _fundingStreamId)
                .WithUserProperty(TemplateVersion, _templateVersion)
                .WithUserProperty(UserId, _userId)
                .WithUserProperty(UserName, _userName);

            overrides?.Invoke(messageBuilder);

            _message = messageBuilder.Build();
        }

        private void AndTheTemplateMapping(TemplateMapping templateMapping)
        {
            _calculationsRepository.GetTemplateMapping(_specificationId, _fundingStreamId)
                .Returns(templateMapping);
        }

        private void AndTheTemplateMetaDataContents(TemplateMetadataContents templateMetadataContents)
        {
            _policies.GetFundingTemplateContents(_fundingStreamId, _templateVersion)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, templateMetadataContents));
        }

        private async Task WhenTheTemplateCalculationsAreApplied()
        {
            await _service.ApplyTemplateCalculation(_message);
        }

        private void AndTheTemplateContentsCalculation(TemplateMappingItem mappingItem,
            TemplateMetadataContents templateMetadataContents,
            TemplateCalculation calculation)
        {
            _calculationQuery.GetTemplateContentsForMappingItem(mappingItem, templateMetadataContents)
                .Returns(calculation);
        }

        private void AndTheTemplateMappingWasUpdated(TemplateMapping templateMapping)
        {
            _calculationsRepository.Received(1)
                .UpdateTemplateMapping(_specificationId, _fundingStreamId, templateMapping);
        }

        private void AndTheJobsStartWasLogged()
        {
            _jobTracker
                .Received(1)
                .TryStartTrackingJob();
        }

        private void AndTheProgressNotificationsWereMade(params int[] itemCount)
        {
            foreach (int count in itemCount)
                _jobTracker
                    .Received(1)
                    .NotifyProgress(count);
        }

        private void AndTheJobCompletionWasLogged(int itemCount)
        {
            _jobTracker
                .Received(1)
                .CompleteTrackingJob("Completed Successfully", itemCount);
        }

        private void AndACalculationRunWasInitialised()
        {
            _instructionAllocationJobCreation
                .Received(1)
                .SendInstructAllocationsToJobService(_specificationId,
                    _userId,
                    _userName,
                    Arg.Is<Trigger>(_ => _.Message == "Assigned Template Calculations" &&
                                         _.EntityId == _specificationId &&
                                         _.EntityType == nameof(Specification))
                    , _correlationId);
        }
    }
}