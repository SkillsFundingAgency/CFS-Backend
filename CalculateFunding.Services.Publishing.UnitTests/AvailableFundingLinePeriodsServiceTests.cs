using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.UnitTests.Profiling;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using SpecificationSummary = CalculateFunding.Common.ApiClient.Specifications.Models.SpecificationSummary;
using TemplateMetadataDistinctFundingLinesContents = CalculateFunding.Common.ApiClient.Policies.Models.TemplateMetadataDistinctFundingLinesContents;
using ProfileVariationPointer = CalculateFunding.Common.ApiClient.Specifications.Models.ProfileVariationPointer;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Changes;
using TemplateMetadataFundingLine = CalculateFunding.Common.ApiClient.Policies.Models.TemplateMetadataFundingLine;
using System.Linq;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class AvailableFundingLinePeriodsServiceTests
    {
        private AvailableFundingLinePeriodsService _availableFundingLinePeriodsService;
        private ISpecificationsApiClient _specificationsApiClient;
        private IProfilingApiClient _profilingApiClient;
        private IPoliciesApiClient _policiesApiClient;

        private string _specificationId;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _templateVersion;

        [TestInitialize]
        public void SetUp()
        {
            _specificationId = NewRandomString();
            _fundingStreamId = NewRandomString();
            _fundingPeriodId = NewRandomString();
            _templateVersion = NewRandomString();

            _specificationsApiClient = CreateSpecificationsApiClient();
            _profilingApiClient = CreateProfilingApiClient();
            _policiesApiClient = CreatePoliciesApiClient();

            _availableFundingLinePeriodsService = CreateService(
                specificationsApiClient: _specificationsApiClient,
                profilingApiClient: _profilingApiClient,
                policiesApiClient: _policiesApiClient);
        }

        [TestMethod]
        public async Task GetAvailableFundingLineProfilePeriodsForVariationPointers_GivenSpecificationSummaryNotFound_ReturnsError()
        {
            //Arrange
            GivenSpecificationSummaryByIdNotFound();

            //Act
            ActionResult<IEnumerable<AvailableVariationPointerFundingLine>> actionResult
                = await WhenGetAvailableFundingLineProfilePeriodsForVariationPointers();

            //Assert
            actionResult
                .Result
                .Should()
                .BeOfType<NotFoundObjectResult>();
        }

        [TestMethod]
        public async Task GetAvailableFundingLineProfilePeriodsForVariationPointers_GivenGetDistinctTemplateMetadataFundingLinesContentsNotFound_ReturnsError()
        {
            //Arrange
            GivenSpecificationSummaryById(NewSpecificationSummary(_ => _
                .WithFundingPeriodId(_fundingPeriodId)
                .WithFundingStreamIds(_fundingStreamId)
                .WithTemplateIds((_fundingStreamId, _templateVersion))));
            AndGetDistinctTemplateMetadataFundingLinesContentsNotFound();

            //Act
            ActionResult<IEnumerable<AvailableVariationPointerFundingLine>> actionResult
                = await WhenGetAvailableFundingLineProfilePeriodsForVariationPointers();

            //Assert
            actionResult
                .Result
                .Should()
                .BeOfType<NotFoundObjectResult>();
        }

        [TestMethod]
        public async Task GetAvailableFundingLineProfilePeriodsForVariationPointers_GivenGetProfilePatternsForFundingStreamAndFundingPeriodNotFound_ReturnsError()
        {
            //Arrange
            GivenSpecificationSummaryById(NewSpecificationSummary(_ => _
                .WithFundingPeriodId(_fundingPeriodId)
                .WithFundingStreamIds(_fundingStreamId)
                .WithTemplateIds((_fundingStreamId, _templateVersion))));
            AndGetDistinctTemplateMetadataFundingLinesContents(NewTemplateMetadataDistinctFundingLinesContents());
            AndGetProfilePatternsForFundingStreamAndFundingPeriodNotFound();

            //Act
            ActionResult<IEnumerable<AvailableVariationPointerFundingLine>> actionResult
                = await WhenGetAvailableFundingLineProfilePeriodsForVariationPointers();

            //Assert
            actionResult
                .Result
                .Should()
                .BeOfType<NotFoundObjectResult>();
        }

        [TestMethod]
        public async Task GetAvailableFundingLineProfilePeriodsForVariationPointers_GivenGetProfileVariationPointersNotFound_ReturnsError()
        {
            //Arrange
            GivenSpecificationSummaryById(NewSpecificationSummary(_ => _
                .WithFundingPeriodId(_fundingPeriodId)
                .WithFundingStreamIds(_fundingStreamId)
                .WithTemplateIds((_fundingStreamId, _templateVersion))));
            AndGetDistinctTemplateMetadataFundingLinesContents(NewTemplateMetadataDistinctFundingLinesContents());
            AndGetProfilePatternsForFundingStreamAndFundingPeriod(new[] { NewFundingStreamPeriodProfilePattern()});
            GivenGetProfileVariationPointersNotFound();

            //Act
            ActionResult<IEnumerable<AvailableVariationPointerFundingLine>> actionResult
                = await WhenGetAvailableFundingLineProfilePeriodsForVariationPointers();

            //Assert
            actionResult
                .Result
                .Should()
                .BeOfType<NotFoundObjectResult>();
        }

        [TestMethod]
        public async Task GetAvailableFundingLineProfilePeriodsForVariationPointers_GivenAllDataExists_ReturnsAvailableFundingLineProfilePeriodsForVariationPointers()
        {
            string fundingLineCode = NewRandomString();
            
            int occurrenceOne = 1;
            string typeValueOne = "May";
            int yearOne = 2021;

            int occurrenceTwo = 1;
            string typeValueTwo = "June";
            int yearTwo = 2021;


            //Arrange
            GivenSpecificationSummaryById(NewSpecificationSummary(_ => _
                .WithFundingPeriodId(_fundingPeriodId)
                .WithFundingStreamIds(_fundingStreamId)
                .WithTemplateIds((_fundingStreamId, _templateVersion))));
            AndGetDistinctTemplateMetadataFundingLinesContents(
                NewTemplateMetadataDistinctFundingLinesContents(_ => _
                    .WithFundingPeriodId(_fundingPeriodId)
                    .WithFundingStreamId(_fundingStreamId)
                    .WithTemplateMetadataFundingLines(
                        NewTemplateMetadataFundingLine(tmf => tmf
                            .WithFundingLineCode(fundingLineCode)))));
            AndGetProfilePatternsForFundingStreamAndFundingPeriod(new[] { 
                NewFundingStreamPeriodProfilePattern(_ => _
                    .WithFundingLineId(fundingLineCode)
                    .WithPeriods(
                        NewProfilePeriodPattern(ppp => ppp
                            .WithOccurence(occurrenceOne)
                            .WithTypeValue(typeValueOne)
                            .WithType(PeriodType.CalendarMonth)
                            .WithYear(yearOne)),
                        NewProfilePeriodPattern(ppp => ppp
                            .WithOccurence(occurrenceTwo)
                            .WithTypeValue(typeValueTwo)
                            .WithType(PeriodType.CalendarMonth)
                            .WithYear(yearTwo)))) });
            GivenGetProfileVariationPointers(new[] { 
                NewProfileVariationPointer(pvp => pvp
                    .WithFundingLineId(fundingLineCode)
                    .WithOccurence(occurrenceOne)
                    .WithTypeValue(typeValueOne)
                    .WithYear(yearOne)) });

            //Act
            ActionResult<IEnumerable<AvailableVariationPointerFundingLine>> actionResult
                = await WhenGetAvailableFundingLineProfilePeriodsForVariationPointers();

            actionResult
                .Value
                .Should()
                .BeOfType<List<AvailableVariationPointerFundingLine>>();

            List<AvailableVariationPointerFundingLine> availableVariationPointerFundingLines 
                = actionResult.Value as List<AvailableVariationPointerFundingLine>;

            availableVariationPointerFundingLines.Count.Should().Be(1);

            AvailableVariationPointerFundingLine availableVariationPointerFundingLine = availableVariationPointerFundingLines.FirstOrDefault();

            availableVariationPointerFundingLine.SelectedPeriod.Year.Should().Be(yearOne);
            availableVariationPointerFundingLine.SelectedPeriod.Period.Should().Be(typeValueOne);
            availableVariationPointerFundingLine.SelectedPeriod.Occurrence.Should().Be(occurrenceOne);
        }

        private void GivenSpecificationSummaryById(SpecificationSummary specificationSummary)
        {
            _specificationsApiClient
                .GetSpecificationSummaryById(_specificationId)
                .Returns(
                    new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, specificationSummary));
        }

        private void GivenSpecificationSummaryByIdNotFound()
        {
            _specificationsApiClient
                .GetSpecificationSummaryById(_specificationId)
                .Returns(
                    new ApiResponse<SpecificationSummary>(HttpStatusCode.NotFound));
        }

        private void AndGetDistinctTemplateMetadataFundingLinesContents(
    TemplateMetadataDistinctFundingLinesContents templateMetadataDistinctFundingLinesContents)
        {
            _policiesApiClient
                .GetDistinctTemplateMetadataFundingLinesContents(_fundingStreamId, _fundingPeriodId, _templateVersion)
                .Returns(
                    new ApiResponse<TemplateMetadataDistinctFundingLinesContents>(HttpStatusCode.OK, templateMetadataDistinctFundingLinesContents));
        }

        private void AndGetDistinctTemplateMetadataFundingLinesContentsNotFound()
        {
            _policiesApiClient
                .GetDistinctTemplateMetadataFundingLinesContents(_fundingStreamId, _fundingPeriodId, _templateVersion)
                .Returns(
                    new ApiResponse<TemplateMetadataDistinctFundingLinesContents>(HttpStatusCode.NotFound));
        }

        private void AndGetProfilePatternsForFundingStreamAndFundingPeriod(IEnumerable<FundingStreamPeriodProfilePattern> fundingStreamPeriodProfilePatterns)
        {
            _profilingApiClient
                .GetProfilePatternsForFundingStreamAndFundingPeriod(_fundingStreamId, _fundingPeriodId)
                .Returns(
                    new ApiResponse<IEnumerable<FundingStreamPeriodProfilePattern>>(HttpStatusCode.OK, fundingStreamPeriodProfilePatterns));
        }

        private void AndGetProfilePatternsForFundingStreamAndFundingPeriodNotFound()
        {
            _profilingApiClient
                .GetProfilePatternsForFundingStreamAndFundingPeriod(_fundingStreamId, _fundingPeriodId)
                .Returns(
                    new ApiResponse<IEnumerable<FundingStreamPeriodProfilePattern>>(HttpStatusCode.NotFound));
        }

        private void GivenGetProfileVariationPointers(IEnumerable<ProfileVariationPointer> profileVariationPointers)
        {
            _specificationsApiClient
                .GetProfileVariationPointers(_specificationId)
                .Returns(
                    new ApiResponse<IEnumerable<ProfileVariationPointer>>(HttpStatusCode.OK, profileVariationPointers));
        }

        private void GivenGetProfileVariationPointersNotFound()
        {
            _specificationsApiClient
                .GetProfileVariationPointers(_specificationId)
                .Returns(
                    new ApiResponse<IEnumerable<ProfileVariationPointer>>(HttpStatusCode.NotFound));
        }

        private async Task<ActionResult<IEnumerable<AvailableVariationPointerFundingLine>>> WhenGetAvailableFundingLineProfilePeriodsForVariationPointers()
                => await _availableFundingLinePeriodsService.GetAvailableFundingLineProfilePeriodsForVariationPointers(_specificationId);

        private SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setup = null)
            => BuildNewModel<SpecificationSummary, SpecificationSummaryBuilder>(setup);

        private TemplateMetadataDistinctFundingLinesContents NewTemplateMetadataDistinctFundingLinesContents(Action<TemplateMetadataDistinctFundingLinesContentsBuilder> setup = null)
            => BuildNewModel<TemplateMetadataDistinctFundingLinesContents, TemplateMetadataDistinctFundingLinesContentsBuilder>(setup);

        private FundingStreamPeriodProfilePattern NewFundingStreamPeriodProfilePattern(Action<FundingStreamPeriodProfilePatternBuilder> setup = null)
            => BuildNewModel<FundingStreamPeriodProfilePattern, FundingStreamPeriodProfilePatternBuilder>(setup);

        private ProfileVariationPointer NewProfileVariationPointer(Action<ProfileVariationPointerBuilder> setup = null)
            => BuildNewModel<ProfileVariationPointer, ProfileVariationPointerBuilder>(setup);

        private ProfilePeriodPattern NewProfilePeriodPattern(Action<ProfilePeriodPatternBuilder> setup = null)
            => BuildNewModel<ProfilePeriodPattern, ProfilePeriodPatternBuilder>(setup);

        private TemplateMetadataFundingLine NewTemplateMetadataFundingLine(Action<TemplateMetadataFundingLineBuilder> setup = null)
            => BuildNewModel<TemplateMetadataFundingLine, TemplateMetadataFundingLineBuilder>(setup);
        

        private static AvailableFundingLinePeriodsService CreateService(
            ISpecificationsApiClient specificationsApiClient = null,
            IProfilingApiClient profilingApiClient = null,
            IPoliciesApiClient policiesApiClient = null)
        {
            return new AvailableFundingLinePeriodsService(
                specificationsApiClient ?? CreateSpecificationsApiClient(),
                profilingApiClient ?? CreateProfilingApiClient(),
                policiesApiClient ?? CreatePoliciesApiClient(),
                PublishingResilienceTestHelper.GenerateTestPolicies());
        }

        private static IProfilingApiClient CreateProfilingApiClient()
        {
            return Substitute.For<IProfilingApiClient>();
        }

        private static ISpecificationsApiClient CreateSpecificationsApiClient()
        {
            return Substitute.For<ISpecificationsApiClient>();
        }

        private static IPoliciesApiClient CreatePoliciesApiClient()
        {
            return Substitute.For<IPoliciesApiClient>();
        }

        private T BuildNewModel<T, TB>(Action<TB> setup) where TB : TestEntityBuilder, new()
        {
            dynamic builder = new TB();
            setup?.Invoke(builder);
            return builder.Build();
        }

        private static RandomString NewRandomString() => new RandomString();
        private static RandomNumberBetween NewRandomInteger() => new RandomNumberBetween(1, 1000);

    }
}
