using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Services;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Tests.Common.Builders;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Polly;
using Serilog;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Calcs.UnitTests.Services
{
    [TestClass]
    public class ApplyTemplateCalculationsServiceTests : TemplateMappingTestBase
    {
        private ICreateCalculationService _createCalculationService;
        private ICalculationsRepository _calculationsRepository;
        private IInstructionAllocationJobCreation _instructionAllocationJobCreation;
        private IPoliciesApiClient _policies;
        private ISpecificationsApiClient _specificationApiClient;
        private IGraphRepository _graphRepository;
        private ICalculationService _calculationService;
        private ICacheProvider _cacheProvider;
        private ICodeContextCache _codeContextCache;
        private IJobManagement _jobManagement;

        private string _specificationId;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _correlationId;
        private string _templateVersion;
        private string _previousTemplateVersion;
        private string _userId;
        private string _userName;
        private string _jobId;
        private Message _message;

        private const string SpecificationId = "specification-id";
        private const string FundingStreamId = "fundingstream-id";
        private const string TemplateVersion = "template-version";
        private const string PreviousTemplateVersion = "previous-template-version";
        private const string CorrelationId = "sfa-correlationId";
        private const string UserId = "user-id";
        private const string UserName = "user-name";
        private const string JobId = "jobId";

        private ApplyTemplateCalculationsService _service;

        [TestInitialize]
        public void SetUp()
        {
            _policies = Substitute.For<IPoliciesApiClient>();
            _specificationApiClient = Substitute.For<ISpecificationsApiClient>();
            _graphRepository = Substitute.For<IGraphRepository>();
            _createCalculationService = Substitute.For<ICreateCalculationService>();
            _calculationsRepository = Substitute.For<ICalculationsRepository>();
            _instructionAllocationJobCreation = Substitute.For<IInstructionAllocationJobCreation>();
            _calculationService = Substitute.For<ICalculationService>();
            _cacheProvider = Substitute.For<ICacheProvider>();
            _codeContextCache = Substitute.For<ICodeContextCache>();
            _jobManagement = Substitute.For<IJobManagement>();

            _userId = $"{NewRandomString()}_userId";
            _userName = $"{NewRandomString()}_userName";
            _correlationId = $"{NewRandomString()}_correlationId";
            _specificationId = $"{NewRandomString()}_specificationId";
            _fundingStreamId = $"{NewRandomString()}_fundingStreamId";
            _fundingPeriodId = $"{NewRandomString()}_fundingPeriodId";
            _templateVersion = $"{NewRandomString()}_templateVersion";
            _previousTemplateVersion = $"{NewRandomString()}_previousTemplateVersion";
            _jobId = $"{NewRandomString()}_jobId";

            _calculationsRepository.UpdateTemplateMapping(Arg.Any<string>(),
                    Arg.Any<string>(),
                    Arg.Any<TemplateMapping>())
                .Returns(Task.CompletedTask);

            _service = new ApplyTemplateCalculationsService(_createCalculationService,
                _policies,
                new ResiliencePolicies
                {
                    PoliciesApiClient = Policy.NoOpAsync(),
                    CalculationsRepository = Policy.NoOpAsync(),
                    CacheProviderPolicy = Policy.NoOpAsync(),
                    SpecificationsApiClient = Policy.NoOpAsync()
                },
                _calculationsRepository,
                _jobManagement,
                _instructionAllocationJobCreation,
                Substitute.For<ILogger>(),
                _calculationService,
                _cacheProvider,
                _specificationApiClient,
                _graphRepository,
                _codeContextCache);
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
            AndTheSpecificationIsReturned();
            AndTheTemplateMapping(NewTemplateMapping());

            ThenAnExceptionShouldBeThrownWithMessage(
                $"Did not locate Template Metadata Contents for funding stream id {_fundingStreamId}, funding period id {_fundingPeriodId} and template version {_templateVersion}");
        }

        [TestMethod]
        public void ThrowsExceptionIfCreateCallFailsWhenCreatingCalculationWhichAlreadyExists()
        {
            TemplateMappingItem mappingWithMissingCalculation1 = NewTemplateMappingItem();
            TemplateMapping templateMapping = NewTemplateMapping(_ => _.WithItems(mappingWithMissingCalculation1));
            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(NewFundingLine(fl => fl.WithCalculations(NewTemplateMappingCalculation()))));
            TemplateCalculation templateCalculationOne = NewTemplateMappingCalculation(_ => _.WithName("template calculation 1"));

            string errorMessage = $"Calculation with the same generated source code name already exists in this specification. Calculation Name {templateCalculationOne.Name} and Specification {_specificationId}";
            
            GivenAValidMessage();
            AndTheJobCanBeRun();
            AndTheTemplateMapping(templateMapping);
            AndTheTemplateMetaDataContents(templateMetadataContents);
            AndTheTemplateContentsCalculation(mappingWithMissingCalculation1, templateMetadataContents, templateCalculationOne);
            AndTheSpecificationIsReturned();
            AndTheCalculationCreateResponse(new CreateCalculationResponse { Succeeded = false, ErrorMessage = errorMessage });

            ThenAnExceptionShouldBeThrownWithMessage($"Unable to create new default template calculation for template mapping {errorMessage}");
        }

        [TestMethod]
        public async Task CreatesCalculationsIfOnTemplateMappingButDontExistYet()
        {
            TemplateMappingItem mappingWithMissingCalculation1 = NewTemplateMappingItem();
            TemplateMappingItem mappingWithMissingCalculation2 = NewTemplateMappingItem();
            TemplateMappingItem mappingWithMissingCalculation3 = NewTemplateMappingItem();
            TemplateMappingItem mappingWithMissingCalculation4 = NewTemplateMappingItem();

            TemplateMapping templateMapping = NewTemplateMapping(_ => _.WithItems(mappingWithMissingCalculation1,
                NewTemplateMappingItem(mi => mi.WithCalculationId(NewRandomString())),
                mappingWithMissingCalculation2,
                NewTemplateMappingItem(mi => mi.WithCalculationId(NewRandomString())),

                mappingWithMissingCalculation3,
                NewTemplateMappingItem(mi => mi.WithCalculationId(NewRandomString())),
                mappingWithMissingCalculation4));

            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(NewFundingLine(fl =>
                fl.WithCalculations(
                    NewTemplateMappingCalculation(c1 =>
                    {
                        c1.WithCalculations(NewTemplateMappingCalculation(c4 => c4.WithTemplateCalculationId(4)));
                        c1.WithTemplateCalculationId(1);
                    }),
                    NewTemplateMappingCalculation(c2 => c2.WithTemplateCalculationId(2)),
                    NewTemplateMappingCalculation(c3 => c3.WithTemplateCalculationId(3)),
                    NewTemplateMappingCalculation(c4 => c4.WithTemplateCalculationId(4)
                          .WithType(Common.TemplateMetadata.Enums.CalculationType.Enum)
                          .WithAllowedEnumTypeValues(new List<string>() { "Type1", "Type2", "Type3" })
                          .WithValueFormat(CalculationValueFormat.String))
                    ))));
            TemplateCalculation templateCalculationOne = NewTemplateMappingCalculation(_ => _.WithName("template calculation 1"));
            TemplateCalculation templateCalculationTwo = NewTemplateMappingCalculation(_ => _.WithName("template calculation 2"));
            TemplateCalculation templateCalculationThree = NewTemplateMappingCalculation(_ => _.WithName("template calculation 3"));
            TemplateCalculation templateCalculationFour = NewTemplateMappingCalculation(_ => _.WithName("template calculation 4"));

            string newCalculationId1 = NewRandomString();
            string newCalculationId2 = NewRandomString();
            string newCalculationId3 = NewRandomString();
            string newCalculationId4 = NewRandomString();

            GivenAValidMessage();
            AndTheJobCanBeRun();
            AndTheTemplateMapping(templateMapping);
            AndTheTemplateMetaDataContents(templateMetadataContents);

            CalculationValueType calculationValueTypeOne = templateCalculationOne.ValueFormat.AsMatchingEnum<CalculationValueType>();
            CalculationValueType calculationValueTypeTwo = templateCalculationTwo.ValueFormat.AsMatchingEnum<CalculationValueType>();
            CalculationValueType calculationValueTypeThree = templateCalculationThree.ValueFormat.AsMatchingEnum<CalculationValueType>();
            CalculationValueType calculationValueTypeFour = templateCalculationFour.ValueFormat.AsMatchingEnum<CalculationValueType>();

            AndTheCalculationIsCreatedForRequestMatching(_ => _.Name == templateCalculationOne.Name &&
                                                              _.SourceCode == calculationValueTypeOne.GetDefaultSourceCode() &&
                                                              _.SpecificationId == _specificationId &&
                                                              _.FundingStreamId == _fundingStreamId &&
                                                              _.ValueType.GetValueOrDefault() == calculationValueTypeOne,
                NewCalculation(_ => _.WithId(newCalculationId1)));
            AndTheCalculationIsCreatedForRequestMatching(_ => _.Name == templateCalculationTwo.Name &&
                                                              _.SourceCode == calculationValueTypeTwo.GetDefaultSourceCode() &&
                                                              _.SpecificationId == _specificationId &&
                                                              _.FundingStreamId == _fundingStreamId &&
                                                              _.ValueType.GetValueOrDefault() == calculationValueTypeTwo,
                NewCalculation(_ => _.WithId(newCalculationId2)));
            AndTheCalculationIsCreatedForRequestMatching(_ => _.Name == templateCalculationThree.Name &&
                                                              _.SourceCode == calculationValueTypeThree.GetDefaultSourceCode() &&
                                                              _.SpecificationId == _specificationId &&
                                                              _.FundingStreamId == _fundingStreamId &&
                                                              _.ValueType.GetValueOrDefault() == calculationValueTypeThree,
                NewCalculation(_ => _.WithId(newCalculationId3)));
            AndTheCalculationIsCreatedForRequestMatching(_ => _.Name == templateCalculationFour.Name &&
                                                              _.SourceCode == calculationValueTypeFour.GetDefaultSourceCode() &&
                                                              _.SpecificationId == _specificationId &&
                                                              _.FundingStreamId == _fundingStreamId &&
                                                              _.ValueType.GetValueOrDefault() == calculationValueTypeFour,
                NewCalculation(_ => _.WithId(newCalculationId4)));
            AndTheTemplateContentsCalculation(mappingWithMissingCalculation1, templateMetadataContents, templateCalculationOne);
            AndTheTemplateContentsCalculation(mappingWithMissingCalculation2, templateMetadataContents, templateCalculationTwo);
            AndTheTemplateContentsCalculation(mappingWithMissingCalculation3, templateMetadataContents, templateCalculationThree);
            AndTheTemplateContentsCalculation(mappingWithMissingCalculation4, templateMetadataContents, templateCalculationFour);
            AndTheSpecificationIsReturned();

            await WhenTheTemplateCalculationsAreApplied();

            mappingWithMissingCalculation1
                .CalculationId
                .Should().Be(newCalculationId1);

            mappingWithMissingCalculation2
                .CalculationId
                .Should().Be(newCalculationId2);

            mappingWithMissingCalculation3
                .CalculationId
                .Should().Be(newCalculationId3);

            mappingWithMissingCalculation4
                .CalculationId
                .Should().Be(newCalculationId4);

            AndTheTemplateMappingWasUpdated(templateMapping, 1);
            AndTheJobsStartWasLogged();
            AndTheJobCompletionWasLogged();
            AndACalculationRunWasInitialised();
        }

        [TestMethod]
        public async Task ModifiesCalculationsIfOnTemplateExists()
        {
            uint templateCalculationId1 = (uint)new RandomNumberBetween(1, int.MaxValue);
            uint templateCalculationId2 = (uint)new RandomNumberBetween(1, int.MaxValue);

            string calculationId1 = "calculationId1";
            string calculationId2 = "calculationId2";
            string calculationId3 = "calculationId3";

            string calculationName1 = "calculationName1";
            string calculationName2 = "calculationName2";
            string calculationName3 = "calculationName3";
            string fundingLine = "fundingLine";

            string newCalculationName1 = "newCalculationName1";
            string newCalculationName2 = "newCalculationName2";
            string newFundingLine = "newFundingLine";

            string newCalculationId4 = "newCalculationId4";
            string newCalculationId5 = "newCalculationId5";

            CalculationValueFormat calculationValueFormat1 = CalculationValueFormat.Currency;
            CalculationValueFormat calculationValueFormat2 = CalculationValueFormat.Number;
            CalculationValueType calculationValueType3 = CalculationValueType.Percentage;

            TemplateMappingItem mappingWithMissingCalculation1 = NewTemplateMappingItem();
            TemplateMappingItem mappingWithMissingCalculation2 = NewTemplateMappingItem();
            TemplateMappingItem mappingWithMissingCalculation3 = NewTemplateMappingItem(_ =>
            {
                _.WithCalculationId(calculationId3);
                _.WithName(calculationName3);
            });

            TemplateMapping templateMapping = NewTemplateMapping(_ => _.WithItems(mappingWithMissingCalculation1,
                NewTemplateMappingItem(mi => mi.WithCalculationId(calculationId1).WithTemplateId(templateCalculationId1)),
                mappingWithMissingCalculation2,
                NewTemplateMappingItem(mi => mi.WithCalculationId(calculationId2).WithTemplateId(templateCalculationId2)),
                mappingWithMissingCalculation3));

            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(NewFundingLine(fl =>
                fl.WithName(newFundingLine)
                    .WithCalculations(
                    NewTemplateMappingCalculation(),
                    NewTemplateMappingCalculation(),
                    NewTemplateMappingCalculation(x => x.WithTemplateCalculationId(templateCalculationId1).WithName(newCalculationName1).WithValueFormat(calculationValueFormat1)),
                    NewTemplateMappingCalculation(x => x.WithTemplateCalculationId(templateCalculationId2).WithName(newCalculationName2).WithValueFormat(calculationValueFormat2))))));
            
            TemplateMetadataContents previousTemplateMetadataContents = templateMetadataContents.DeepCopy();
            previousTemplateMetadataContents.RootFundingLines.First().Name = fundingLine;

            TemplateCalculation templateCalculationOne = NewTemplateMappingCalculation(_ => _.WithName("template calculation 1"));
            TemplateCalculation templateCalculationTwo = NewTemplateMappingCalculation(_ => _.WithName("template calculation 2"));

            List<Calculation> calculations = new List<Calculation>
            {
                NewCalculation(_ => _.WithId(calculationId1)
                                    .WithCurrentVersion(
                                        NewCalculationVersion(x=>x.WithCalculationId(calculationId1).WithName(calculationName1)))),
                NewCalculation(_ => _.WithId(calculationId2)
                                    .WithCurrentVersion(
                                        NewCalculationVersion(x=>x.WithCalculationId(calculationId2).WithName(calculationName2)))),
            };

            Calculation missingCalculation = NewCalculation(_ => _.WithId(calculationId3)
                                    .WithCurrentVersion(
                                        NewCalculationVersion(x =>
                                        {
                                            x.WithName(calculationName3);
                                            x.WithValueType(calculationValueType3);
                                        })));

            GivenAValidMessage(_previousTemplateVersion);
            AndTheJobCanBeRun();
            AndTheTemplateMapping(templateMapping);
            AndTheTemplateMetaDataContents(templateMetadataContents);
            AndThePreviousTemplateMetaDataContents(previousTemplateMetadataContents);

            CalculationValueType calculationValueTypeOne = templateCalculationOne.ValueFormat.AsMatchingEnum<CalculationValueType>();
            CalculationValueType calculationValueTypeTwo = templateCalculationTwo.ValueFormat.AsMatchingEnum<CalculationValueType>();

            AndTheCalculationIsCreatedForRequestMatching(_ => _.Name == templateCalculationOne.Name &&
                                                              _.SourceCode == calculationValueTypeOne.GetDefaultSourceCode() &&
                                                              _.SpecificationId == _specificationId &&
                                                              _.FundingStreamId == _fundingStreamId &&
                                                              _.ValueType.GetValueOrDefault() == calculationValueTypeOne,
                NewCalculation(_ => _.WithId(newCalculationId4)));
            AndTheCalculationIsEditedForRequestMatching(_ => _.Name == newCalculationName1 &&
                                                            _.ValueType.GetValueOrDefault() == calculationValueFormat1.AsMatchingEnum<CalculationValueType>() &&
                                                            _.Description == null &&
                                                            _.SourceCode == null,
                calculationId1);
            AndTheCalculationIsCreatedForRequestMatching(_ => _.Name == templateCalculationTwo.Name &&
                                                              _.SourceCode == calculationValueTypeTwo.GetDefaultSourceCode() &&
                                                              _.SpecificationId == _specificationId &&
                                                              _.FundingStreamId == _fundingStreamId &&
                                                              _.ValueType.GetValueOrDefault() == calculationValueTypeTwo,
                NewCalculation(_ => _.WithId(newCalculationId5)));
            AndTheCalculationIsEditedForRequestMatching(_ => _.Name == newCalculationName2 &&
                                                _.ValueType.GetValueOrDefault() == calculationValueFormat2.AsMatchingEnum<CalculationValueType>() &&
                                                _.Description == null &&
                                                _.SourceCode == null,
                calculationId2);

            AndTheTemplateContentsCalculation(mappingWithMissingCalculation1, templateMetadataContents, templateCalculationOne);
            AndTheTemplateContentsCalculation(mappingWithMissingCalculation2, templateMetadataContents, templateCalculationTwo);

            AndMissingCalculation(calculationId3, missingCalculation);
            AndTheCalculations(calculations);
            AndTheSpecificationIsReturned();
            AndTheCalculationCodeOnCalculationChangeReturned(calculationId1, newCalculationName1, calculationName1, _specificationId, calculations.Where(_ => _.Id == calculationId1));

            await WhenTheTemplateCalculationsAreApplied();

            mappingWithMissingCalculation1
                .CalculationId
                .Should().Be(newCalculationId4);

            mappingWithMissingCalculation2
                .CalculationId
                .Should().Be(newCalculationId5);

            AndTheCalculationCodeOnCalculationChangeUpdated(newCalculationName1, calculationName1, _specificationId, 1);
            AndTheCalculationCodeOnCalculationChangeUpdated(newCalculationName2, calculationName2, _specificationId, 1);
            AndTheCalculationCodeOnCalculationChangeUpdated(newFundingLine, fundingLine, _specificationId, 1);
            AndUpdateBuildProjectCalled(_specificationId, 2);
            AndTheTemplateMappingWasUpdated(templateMapping, 1);
            AndTheJobsStartWasLogged();
            AndTheJobCompletionWasLogged();
            AndACalculationRunWasInitialised();
            AndACodeContextUpdateJobWasQueued();
        }

        [TestMethod]
        public async Task DoesNotModifiesCalculationsIfOnTemplateExistsAndHasSameValues()
        {
            uint templateCalculationId1 = (uint)new RandomNumberBetween(1, int.MaxValue);
            uint templateCalculationId2 = (uint)new RandomNumberBetween(1, int.MaxValue);
            uint templateCalculationId3 = (uint)new RandomNumberBetween(1, int.MaxValue);
            uint templateCalculationId4 = (uint)new RandomNumberBetween(1, int.MaxValue);

            string calculationId1 = NewRandomString();
            string calculationId2 = NewRandomString();

            string calculationName1 = NewRandomString();
            string calculationName2 = NewRandomString();

            string newCalculationId1 = NewRandomString();
            string newCalculationId2 = NewRandomString();

            CalculationValueFormat calculationValueFormat1 = CalculationValueFormat.Currency;
            CalculationValueFormat calculationValueFormat2 = CalculationValueFormat.Number;

            TemplateMappingItem mappingWithMissingCalculation1 = NewTemplateMappingItem();
            TemplateMappingItem mappingWithMissingCalculation2 = NewTemplateMappingItem();

            TemplateMapping templateMapping = NewTemplateMapping(_ => _.WithItems(mappingWithMissingCalculation1,
                mappingWithMissingCalculation2));

            TemplateMetadataContents templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(NewFundingLine(fl =>
                fl.WithCalculations(
                    NewTemplateMappingCalculation(x => x.WithTemplateCalculationId(templateCalculationId1).WithName(calculationName1).WithValueFormat(calculationValueFormat1)),
                    NewTemplateMappingCalculation(x => x.WithTemplateCalculationId(templateCalculationId2).WithName(calculationName2).WithValueFormat(calculationValueFormat2)),
                    NewTemplateMappingCalculation(x => x.WithTemplateCalculationId(templateCalculationId3)),
                    NewTemplateMappingCalculation(x => x.WithTemplateCalculationId(templateCalculationId4))))));
            TemplateCalculation templateCalculationOne = NewTemplateMappingCalculation(_ => _.WithName("template calculation 1"));
            TemplateCalculation templateCalculationTwo = NewTemplateMappingCalculation(_ => _.WithName("template calculation 2"));

            List<Calculation> calculations = new List<Calculation>
            {
                NewCalculation(_ => _.WithId(calculationId1)
                                    .WithCurrentVersion(
                                        NewCalculationVersion(x =>
                                            x.WithCalculationId(calculationId1).WithName(calculationName1).WithValueType(calculationValueFormat1.AsMatchingEnum<CalculationValueType>())))),
                NewCalculation(_ => _.WithId(calculationId2)
                                    .WithCurrentVersion(
                                        NewCalculationVersion(x=>x.WithCalculationId(calculationId2).WithName(calculationName2).WithValueType(calculationValueFormat2.AsMatchingEnum<CalculationValueType>())))),
            };

            GivenAValidMessage();
            AndTheJobCanBeRun();
            AndTheTemplateMapping(templateMapping);
            AndTheTemplateMetaDataContents(templateMetadataContents);

            CalculationValueType calculationValueTypeOne = templateCalculationOne.ValueFormat.AsMatchingEnum<CalculationValueType>();
            CalculationValueType calculationValueTypeTwo = templateCalculationTwo.ValueFormat.AsMatchingEnum<CalculationValueType>();

            AndTheCalculationIsCreatedForRequestMatching(_ => _.Name == templateCalculationOne.Name &&
                                                              _.SourceCode == calculationValueTypeOne.GetDefaultSourceCode() &&
                                                              _.SpecificationId == _specificationId &&
                                                              _.FundingStreamId == _fundingStreamId &&
                                                              _.ValueType.GetValueOrDefault() == calculationValueTypeOne,
                NewCalculation(_ => _.WithId(newCalculationId1)));
            AndTheCalculationIsCreatedForRequestMatching(_ => _.Name == templateCalculationTwo.Name &&
                                                              _.SourceCode == calculationValueTypeTwo.GetDefaultSourceCode() &&
                                                              _.SpecificationId == _specificationId &&
                                                              _.FundingStreamId == _fundingStreamId &&
                                                              _.ValueType.GetValueOrDefault() == calculationValueTypeTwo,
                NewCalculation(_ => _.WithId(newCalculationId2)));
            AndTheTemplateContentsCalculation(mappingWithMissingCalculation1, templateMetadataContents, templateCalculationOne);
            AndTheTemplateContentsCalculation(mappingWithMissingCalculation2, templateMetadataContents, templateCalculationTwo);

            AndTheCalculations(calculations);
            AndTheSpecificationIsReturned();

            await WhenTheTemplateCalculationsAreApplied();

            mappingWithMissingCalculation1
                .CalculationId
                .Should().Be(newCalculationId1);

            mappingWithMissingCalculation2
                .CalculationId
                .Should().Be(newCalculationId2);

            AndTheTemplateMappingWasUpdated(templateMapping, 1);
            AndTheJobsStartWasLogged();
            AndTheJobCompletionWasLogged();
            AndACalculationRunWasInitialised();

            AndCalculationEdited(_ => _.Name == calculationName1 &&
                                                            _.ValueType.GetValueOrDefault() == calculationValueFormat1.AsMatchingEnum<CalculationValueType>() &&
                                                            _.Description == null &&
                                                            _.SourceCode == null,
                calculationId1, 0);

            AndCalculationEdited(_ => _.Name == calculationName2 &&
                                                _.ValueType.GetValueOrDefault() == calculationValueFormat2.AsMatchingEnum<CalculationValueType>() &&
                                                _.Description == null &&
                                                _.SourceCode == null,
                calculationId2, 0);
        }

        private void AndTheCalculationIsCreatedForRequestMatching(Expression<Predicate<CalculationCreateModel>> createModelMatching, Calculation calculation)
        {
            _createCalculationService.CreateCalculation(Arg.Is(_specificationId),
                    Arg.Is(createModelMatching),
                    Arg.Is(CalculationNamespace.Template),
                    Arg.Is(Models.Calcs.CalculationType.Template),
                    Arg.Is<Reference>(_ => _.Id == _userId &&
                                           _.Name == _userName),
                    Arg.Is(_correlationId),
                    Arg.Any<CalculationDataType>(),
                    Arg.Is(false),
                    Arg.Any<IEnumerable<string>>())
                .Returns(new CreateCalculationResponse
                {
                    Succeeded = true,
                    Calculation = calculation
                });
        }

        private void AndTheSpecificationIsReturned()
        {
            _specificationApiClient.GetSpecificationSummaryById(Arg.Is(_specificationId))
                .Returns(new ApiResponse<SpecificationSummary>
                (
                    HttpStatusCode.OK,
                    new SpecificationSummary
                    {
                        Id = _specificationId,
                        FundingPeriod = new Reference(_fundingPeriodId, "")
                    }
                )); ;
        }

        private void AndTheCalculationIsEditedForRequestMatching(Expression<Predicate<CalculationEditModel>> editModelMatching, string calculationId)
        {
            _calculationService.EditCalculation(Arg.Is(_specificationId),
                Arg.Is(calculationId),
                Arg.Is(editModelMatching),
                Arg.Is<Reference>(_ => _.Id == _userId &&
                                       _.Name == _userName),
                Arg.Is(_correlationId),
                Arg.Is(false),
                Arg.Is(true),
                Arg.Is(true),
                Arg.Any<bool>(),
                Arg.Is(true),
                Arg.Is(CalculationEditMode.System),
                Arg.Any<Calculation>())
                .Returns(new OkObjectResult(null));
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

        private void GivenAValidMessage(string previousTemplateVersion = null)
        {
            GivenTheOtherwiseValidMessage(previousTemplateVersion: previousTemplateVersion);
        }

        private void GivenTheOtherwiseValidMessage(Action<MessageBuilder> overrides = null, string previousTemplateVersion = null)
        {
            MessageBuilder messageBuilder = new MessageBuilder()
                .WithUserProperty(SpecificationId, _specificationId)
                .WithUserProperty(CorrelationId, _correlationId)
                .WithUserProperty(FundingStreamId, _fundingStreamId)
                .WithUserProperty(TemplateVersion, _templateVersion)
                .WithUserProperty(UserId, _userId)
                .WithUserProperty(UserName, _userName)
                .WithUserProperty(JobId, _jobId);

            if (!string.IsNullOrWhiteSpace(previousTemplateVersion))
            {
                messageBuilder.WithUserProperty(PreviousTemplateVersion, previousTemplateVersion);
            }

            overrides?.Invoke(messageBuilder);

            _message = messageBuilder.Build();
        }

        private void AndTheJobCanBeRun()
        {
            _jobManagement.RetrieveJobAndCheckCanBeProcessed(_jobId)
                .Returns(new JobViewModel { Id = _jobId });
        }

        private void AndTheCalculationCreateResponse(CreateCalculationResponse createCalculationResponse)
        {
            _createCalculationService.CreateCalculation(Arg.Any<string>(),
                Arg.Any<CalculationCreateModel>(),
                Arg.Any<CalculationNamespace>(),
                Arg.Any<Models.Calcs.CalculationType>(),
                Arg.Any<Reference>(),
                Arg.Any<string>(),
                Arg.Any<CalculationDataType>(),
                false,
                Arg.Any<IEnumerable<string>>())
            .Returns(createCalculationResponse);
        }

        private void AndTheTemplateMapping(TemplateMapping templateMapping)
        {
            _calculationsRepository.GetTemplateMapping(_specificationId, _fundingStreamId)
                .Returns(templateMapping);
        }

        private void AndTheTemplateMetaDataContents(TemplateMetadataContents templateMetadataContents)
        {
            _policies.GetFundingTemplateContents(_fundingStreamId, _fundingPeriodId, _templateVersion)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, templateMetadataContents));
        }

        private void AndThePreviousTemplateMetaDataContents(TemplateMetadataContents templateMetadataContents)
        {
            _policies.GetFundingTemplateContents(_fundingStreamId, _fundingPeriodId, _previousTemplateVersion)
                .Returns(new ApiResponse<TemplateMetadataContents>(HttpStatusCode.OK, templateMetadataContents));
        }

        private async Task WhenTheTemplateCalculationsAreApplied()
        {
            await _service.Run(_message);
        }

        private void AndTheTemplateContentsCalculation(TemplateMappingItem mappingItem,
            TemplateMetadataContents templateMetadataContents,
            TemplateCalculation calculation)
        {
            calculation.TemplateCalculationId = mappingItem.TemplateId;

            FundingLine fundingLine = templateMetadataContents.RootFundingLines.First();

            fundingLine.Calculations = fundingLine.Calculations.Concat(new[]
            {
                calculation
            });
        }

        private void AndTheCalculations(IEnumerable<Calculation> calculations)
        {
            _calculationsRepository.GetCalculationsBySpecificationId(_specificationId)
                .Returns(calculations);
        }

        private void AndMissingCalculation(string calculationId, Calculation calculation)
        {
            _calculationsRepository.GetCalculationById(calculationId)
                .Returns(calculation);
        }

        private void AndTheCalculationCodeOnCalculationChangeReturned(string calculationId, string currentName, string previousName, string specificationId, IEnumerable<Calculation> updatedCalculations)
        {
            _calculationService
                .UpdateCalculationCodeOnCalculationOrFundinglineChange(Arg.Is(previousName),
                    Arg.Is(currentName),
                    Arg.Is(specificationId),
                    Arg.Any<string>(),
                    Arg.Any<Reference>(),
                    false
                )
                .Returns(updatedCalculations);
        }

        private void AndTheCalculationCodeOnCalculationChangeUpdated(string currentName, string previousName, string specificationId, int numberOfCalls)
        {
            _calculationService.Received(numberOfCalls)
                .UpdateCalculationCodeOnCalculationOrFundinglineChange(Arg.Is(previousName),
                    Arg.Is(currentName),
                    Arg.Is(specificationId),
                    Arg.Any<string>(),
                    Arg.Any<Reference>(),
                    false
                );
        }

        private void AndUpdateBuildProjectCalled(string specificationId, int numberOfCalls)
        {
            _calculationService.Received(numberOfCalls)
                .UpdateBuildProject(Arg.Is<SpecificationSummary>(_ => _.Id == specificationId));
        }

        private void AndTheTemplateMappingWasUpdated(TemplateMapping templateMapping, int numberOfCalls)
        {
            _calculationsRepository.Received(numberOfCalls)
                .UpdateTemplateMapping(_specificationId, _fundingStreamId, templateMapping);
        }

        private void AndTheJobsStartWasLogged()
        {
            _jobManagement
                .Received(1)
                .UpdateJobStatus(_jobId, 0, 0, null, null);
        }

        private void AndTheJobCompletionWasLogged()
        {
            _jobManagement
                .Received(1)
                .UpdateJobStatus(_jobId, 0, 0, true, null);
        }

        private void AndCalculationEdited(Expression<Predicate<CalculationEditModel>> editModelMatching, string calculationId, int requiredNumberOfCalls)
        {
            _calculationService
                .Received(requiredNumberOfCalls)
                .EditCalculation(Arg.Is(_specificationId),
                    Arg.Is(calculationId),
                    Arg.Is(editModelMatching),
                    Arg.Is<Reference>(_ => _.Id == _userId &&
                                           _.Name == _userName),
                    Arg.Is(_correlationId));
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
                                         _.EntityType == "Specification")
                    , _correlationId);
        }

        private void AndACodeContextUpdateJobWasQueued()
        {
            _codeContextCache
                .Received(1)
                .QueueCodeContextCacheUpdate(_specificationId);
        }
    }
}