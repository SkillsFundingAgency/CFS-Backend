using System;
using System.Net;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Api.Profiling.Tests
{
    [TestClass]
    public class ProfileRequestValidatorTests
    {
        [TestMethod]
        [TestCategory("UnitTest")]
        public void ProfileRequestValidator_ShouldReturnPatternWithValidRequest()
        {
            // arrange
            FundingStreamPeriodProfilePattern pattern = new FundingStreamPeriodProfilePattern(
                "AY-1819",
                "PSG",
                "FL1",
                new DateTime(2018, 1, 1),
                new DateTime(2019, 1, 1),
                false,
                false,
                false,
                new[]
                {
                    new ProfilePeriodPattern(
                        PeriodType.CalendarMonth,
                        "Oct",
                        new DateTime(2018, 1, 1),
                        new DateTime(2019, 1, 1),
                        2018,
                        1,
                        "DP1819",
                        100m)
                },
                "FSP-ProfilePattern1",
                "FSP-ProfilePatternDescription1",
                RoundingStrategy.RoundDown);

            ProfileRequest request = new ProfileRequest(
                "PSG",
                "AY-1819",
                "FL1",
                200);

            // act
            ProfileValidationResult validationResult = ProfileRequestValidator.ValidateRequestAgainstPattern(request, pattern);

            // assert
            validationResult
                .Code
                .Should().Be(HttpStatusCode.OK);
        }


        [TestMethod]
        [TestCategory("UnitTest")]
        public void ProfileRequestValidator_ShouldReturnPatternWithValidRequestAAC1920()
        {
            // arrange
            FundingStreamPeriodProfilePattern pattern = new FundingStreamPeriodProfilePattern(
                "AY-1819",
                "PSG",
                "FL1",
                new DateTime(2019, 8, 1),
                new DateTime(2020, 7, 31),
                false,
                false,
                false,
                new[]
                {
                    new ProfilePeriodPattern(
                        PeriodType.CalendarMonth,
                        "Aug",
                        new DateTime(2019, 8, 1),
                        new DateTime(2019, 8, 31),
                        2019,
                        1,
                        "FY1920",
                        12.56m),

                    new ProfilePeriodPattern(
                        PeriodType.CalendarMonth,
                        "Apr",
                        new DateTime(2020, 4, 1),
                        new DateTime(2020, 4, 30),
                        2020,
                        1,
                        "FY2021",
                        12.56m)
                },
                "FSP-ProfilePattern1",
                "FSP-ProfilePatternDescription1",
                RoundingStrategy.RoundDown);

            ProfileRequest request = new ProfileRequest(
                "PSG",
                "AY-1819",
                "FL1",
                200);

            // act
            ProfileValidationResult validationResult = ProfileRequestValidator.ValidateRequestAgainstPattern(request, pattern);

            // assert
            validationResult
                .Code
                .Should().Be(HttpStatusCode.OK);
        }


        [TestMethod]
        [TestCategory("UnitTest")]
        public void ProfileRequestValidator_ShouldReturnBadRequestWithNullFsp()
        {
            // arrange
            ProfileRequest request = new ProfileRequest(
                null,
                null,
                null,
                0
            );

            // act
            ProfileValidationResult validationResult = ProfileRequestValidator.ValidateRequestAgainstPattern(request, null);

            // assert
            validationResult
                .Code
                .Should().Be(HttpStatusCode.BadRequest);
        }

        [TestMethod]
        [TestCategory("UnitTest")]
        public void ProfileRequestValidator_ShouldReturnBadRequestWithNotFoundFsp()
        {
            // arrange
            ProfileRequest request = new ProfileRequest(
                null,
                "ABC-123",
                null,
                0);

            // act
            ProfileValidationResult validationResult = ProfileRequestValidator.ValidateRequestAgainstPattern(request, null);

            // assert
            validationResult
                .Code
                .Should().Be(HttpStatusCode.NotFound);
        }
    }
}