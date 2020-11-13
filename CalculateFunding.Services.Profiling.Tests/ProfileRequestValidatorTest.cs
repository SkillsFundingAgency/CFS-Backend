using System;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Repositories;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Profiling.Tests
{
    public class ProfileRequestValidatorTest
    {
        protected Mock<IProfilePatternRepository> ProfilePatterns;
        protected ValidationResult Result;
        
        [TestInitialize]
        public void ProfileRequestValidatorTestSetUp()
        {
            ProfilePatterns = new Mock<IProfilePatternRepository>();
        }

        protected void ThenThereAreNoValidationErrors()
        {
            Result
                .IsValid
                .Should()
                .BeTrue();
        }

        protected void ThenTheValidationResultsContainsTheErrors(params (string, string)[] errors)
        {
            Result.Errors.Count
                .Should()
                .Be(errors.Length);
            
            foreach ((string, string) error in errors)
            {
                Result.Errors
                    .Should()
                    .Contain(_ => _.PropertyName == error.Item1 &&
                                  _.ErrorMessage == error.Item2, 
                        $"Expected validation errors to contain {error.Item1}:{error.Item2}");
            }    
        }

        protected void GivenTheExistingProfilePattern(FundingStreamPeriodProfilePattern pattern)
        {
            ProfilePatterns.Setup(_ => _.GetProfilePattern(pattern.FundingPeriodId,
                    pattern.FundingStreamId,
                    pattern.FundingLineId,
                    pattern.ProfilePatternKey))
                .ReturnsAsync(pattern);
        }

        protected FundingStreamPeriodProfilePattern NewProfilePattern(Action<FundingStreamPeriodProfilePatternBuilder> setUp = null)
        {
            FundingStreamPeriodProfilePatternBuilder builder = new FundingStreamPeriodProfilePatternBuilder()
                .WithPeriods(NewPeriod(pp => pp.WithPercentage(99.9996M)));

            setUp?.Invoke(builder);
            
            return builder.Build();
        }

        protected ProfilePeriodPattern NewPeriod(Action<ProfilePeriodPatternBuilder> setUp = null)
        {
            ProfilePeriodPatternBuilder patternBuilder = new ProfilePeriodPatternBuilder();

            setUp?.Invoke(patternBuilder);
            
            return patternBuilder.Build();
        }

        protected ProfilePeriodPattern[] NewPeriods(params ProfilePeriodPattern[] periods) => periods;

        protected string NewRandomString() => new RandomString();
    }
}