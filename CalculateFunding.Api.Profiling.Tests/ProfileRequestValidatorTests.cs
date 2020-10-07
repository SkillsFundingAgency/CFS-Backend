namespace CalculateFunding.Api.Profiling.Tests
{
    using System;
    using System.Net;
    using CalculateFunding.Services.Profiling.Models;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Services.Profiling.Services;

    [TestClass]
    public class ProfileRequestValidatorTests
    {
        [TestMethod, TestCategory("UnitTest")]
        public void ProfileRequestValidator_ShouldReturnPatternWithValidRequest()
        {
            // arrange
            FundingStreamPeriodProfilePattern pattern = new FundingStreamPeriodProfilePattern(
                fundingPeriodId: "AY-1819",
                fundingStreamId: "PSG",
                fundingLineId: "FL1",
                fundingStreamPeriodStartDate: new DateTime(2018, 1, 1),
                fundingStreamPeriodEndDate: new DateTime(2019, 1, 1),
                reProfilePastPeriods: false,
                calculateBalancingPayment: false,
                allowUserToEditProfilePattern: false,
                profilePattern: new[]
                {
                    new ProfilePeriodPattern(
                        periodType: PeriodType.CalendarMonth,
                        period: "Oct",
                        periodStartDate: new DateTime(2018, 1, 1),
                        periodEndDate: new DateTime(2019, 1, 1),
                        periodYear: 2018,
                        occurrence: 1,
                        distributionPeriod: "DP1819",
                        periodPatternPercentage: 100m)
                },
                profilePatternDisplayName: "FSP-ProfilePattern1",
                profilePatternDescription: "FSP-ProfilePatternDescription1");

            ProfileRequest request = new ProfileRequest(
                fundingStreamId: "PSG",
                fundingPeriodId: "AY-1819",
                fundingLineCode: "FL1",
                fundingValue: 200);

            // act
            ProfileValidationResult validationResult = ProfileRequestValidator.ValidateRequestAgainstPattern(request, pattern);

            // assert
            validationResult
                .Code
                .Should().Be(HttpStatusCode.OK);
        }


        [TestMethod, TestCategory("UnitTest")]
        public void ProfileRequestValidator_ShouldReturnPatternWithValidRequestAAC1920()
        {
            // arrange
            FundingStreamPeriodProfilePattern pattern = new FundingStreamPeriodProfilePattern(
                fundingPeriodId: "AY-1819",
                fundingStreamId: "PSG",
                fundingLineId: "FL1",
                fundingStreamPeriodStartDate: new DateTime(2019, 8, 1),
                fundingStreamPeriodEndDate: new DateTime(2020, 7, 31),
                reProfilePastPeriods: false,
                calculateBalancingPayment: false,
                allowUserToEditProfilePattern: false,
                profilePattern: new[]
                {
                    new ProfilePeriodPattern(
                        periodType: PeriodType.CalendarMonth,
                        period: "Aug",
                        periodStartDate: new DateTime(2019, 8, 1),
                        periodEndDate: new DateTime(2019, 8, 31),
                        periodYear: 2019,
                        occurrence: 1,
                        distributionPeriod: "FY1920",
                        periodPatternPercentage: 12.56m),

                    new ProfilePeriodPattern(
                    periodType: PeriodType.CalendarMonth,
                    period: "Apr",
                    periodStartDate: new DateTime(2020, 4, 1),
                    periodEndDate: new DateTime(2020, 4, 30),
                    periodYear: 2020,
                    occurrence: 1,
                    distributionPeriod: "FY2021",
                    periodPatternPercentage: 12.56m)
                },
                profilePatternDisplayName: "FSP-ProfilePattern1",
                profilePatternDescription: "FSP-ProfilePatternDescription1");

            ProfileRequest request = new ProfileRequest(
                 fundingStreamId: "PSG",
                fundingPeriodId: "AY-1819",
                fundingLineCode: "FL1",
                fundingValue: 200);

            // act
            ProfileValidationResult validationResult = ProfileRequestValidator.ValidateRequestAgainstPattern(request, pattern);

            // assert
            validationResult
                .Code
                .Should().Be(HttpStatusCode.OK);
        }


        [TestMethod, TestCategory("UnitTest")]
        public void ProfileRequestValidator_ShouldReturnBadRequestWithNullFsp()
        {
            // arrange
            ProfileRequest request = new ProfileRequest(
                 fundingStreamId: null,
                fundingPeriodId: null,
                fundingLineCode: null,
                fundingValue: 0
                );

            // act
            ProfileValidationResult validationResult = ProfileRequestValidator.ValidateRequestAgainstPattern(request, null);

            // assert
            validationResult
                .Code
                .Should().Be(HttpStatusCode.BadRequest);
        }

        [TestMethod, TestCategory("UnitTest")]
        public void ProfileRequestValidator_ShouldReturnBadRequestWithNotFoundFsp()
        {
            // arrange
            ProfileRequest request = new ProfileRequest(
               fundingStreamId: null,
                fundingPeriodId: "ABC-123",
                fundingLineCode: null,
                fundingValue: 0);

            // act
            ProfileValidationResult validationResult = ProfileRequestValidator.ValidateRequestAgainstPattern(request, null);

            // assert
            validationResult
                .Code
                .Should().Be(HttpStatusCode.NotFound);
        }


    }
}