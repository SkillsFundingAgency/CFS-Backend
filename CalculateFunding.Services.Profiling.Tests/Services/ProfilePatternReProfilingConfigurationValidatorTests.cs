using System;
using System.Linq;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.ReProfilingStrategies;
using CalculateFunding.Services.Profiling.Services;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Profiling.Tests.Services
{
    [TestClass]
    public class ProfilePatternReProfilingConfigurationValidatorTests
    {
        private ProfilePatternReProfilingConfigurationValidator _validator;
        private Mock<IReProfilingStrategyLocator> _strategies;

        [TestInitialize]
        public void SetUp()
        {
            _strategies = new Mock<IReProfilingStrategyLocator>();

            _validator = new ProfilePatternReProfilingConfigurationValidator(_strategies.Object);
        }

        [TestMethod]
        public void FailsValidationIfDecreaseStrategyDoesntExist()
        {
            string increaseKey = NewRandomString();
            string decreaseKey = NewRandomString();
            string sameKey = NewRandomString();

            FundingStreamPeriodProfilePattern pattern = NewFundingStreamPeriodProfilePattern(_ => _.WithProfilePatternReProfilingConfiguration(
                NewProfilePatternReProfilingConfiguration(rp => rp.WithDecreasedAmountStrategyKey(decreaseKey)
                    .WithIncreasedAmountStrategyKey(increaseKey)
                    .WithSameAmountStrategyKey(sameKey))));

            GivenTheStrategiesExist(increaseKey, sameKey);

            ValidationResult validationResult = WhenThePatternIsValidated(pattern);

            ThenTheValidationResultsAre(validationResult, ("ReProfilingConfiguration.DecreasedAmountStrategyKey", "No matching strategy exists"));
        }

        [TestMethod]
        public void FailsValidationIfIncreaseStrategyDoesntExist()
        {
            string increaseKey = NewRandomString();
            string decreaseKey = NewRandomString();
            string sameKey = NewRandomString();

            FundingStreamPeriodProfilePattern pattern = NewFundingStreamPeriodProfilePattern(_ => _.WithProfilePatternReProfilingConfiguration(
                NewProfilePatternReProfilingConfiguration(rp => rp.WithDecreasedAmountStrategyKey(decreaseKey)
                    .WithIncreasedAmountStrategyKey(increaseKey)
                    .WithSameAmountStrategyKey(sameKey))));

            GivenTheStrategiesExist(decreaseKey, sameKey);

            ValidationResult validationResult = WhenThePatternIsValidated(pattern);

            ThenTheValidationResultsAre(validationResult, ("ReProfilingConfiguration.IncreasedAmountStrategyKey", "No matching strategy exists"));
        }

        [TestMethod]
        public void FailsValidationIfSameStrategyDoesntExist()
        {
            string increaseKey = NewRandomString();
            string decreaseKey = NewRandomString();
            string sameKey = NewRandomString();

            FundingStreamPeriodProfilePattern pattern = NewFundingStreamPeriodProfilePattern(_ => _.WithProfilePatternReProfilingConfiguration(
                NewProfilePatternReProfilingConfiguration(rp => rp.WithDecreasedAmountStrategyKey(decreaseKey)
                    .WithIncreasedAmountStrategyKey(increaseKey)
                    .WithSameAmountStrategyKey(sameKey))));

            GivenTheStrategiesExist(increaseKey, decreaseKey);

            ValidationResult validationResult = WhenThePatternIsValidated(pattern);

            ThenTheValidationResultsAre(validationResult, ("ReProfilingConfiguration.SameAmountStrategyKey", "No matching strategy exists"));
        }

        private ValidationResult WhenThePatternIsValidated(FundingStreamPeriodProfilePattern pattern)
            => _validator.Validate(pattern);

        private void GivenTheStrategiesExist(params string[] strategies)
        {
            foreach (string strategy in strategies)
            {
                _strategies.Setup(_ => _.HasStrategy(strategy))
                    .Returns(true);
            }
        }

        private void ThenTheValidationResultsAre(ValidationResult validationResult,
            params (string property, string message)[] expectedResults)
        {
            validationResult.Errors.Count
                .Should()
                .Be(expectedResults?.Length ?? 0);

            foreach ((string property, string message) expectedResult in expectedResults)
            {
                validationResult
                    .Errors
                    .Count(_ => _.PropertyName == expectedResult.property &&
                                _.ErrorMessage == expectedResult.message)
                    .Should()
                    .Be(1);
            }
        }

        private FundingStreamPeriodProfilePattern NewFundingStreamPeriodProfilePattern(Action<FundingStreamPeriodProfilePatternBuilder> setUp = null)
        {
            FundingStreamPeriodProfilePatternBuilder fundingStreamPeriodProfilePatternBuilder = new FundingStreamPeriodProfilePatternBuilder();

            setUp?.Invoke(fundingStreamPeriodProfilePatternBuilder);

            return fundingStreamPeriodProfilePatternBuilder.Build();
        }

        private ProfilePatternReProfilingConfiguration NewProfilePatternReProfilingConfiguration(Action<ProfilePatternReProfilingConfigurationBuilder> setUp = null)
        {
            ProfilePatternReProfilingConfigurationBuilder reProfilingConfigurationBuilder = new ProfilePatternReProfilingConfigurationBuilder()
                .WithIsEnabled(true);

            setUp?.Invoke(reProfilingConfigurationBuilder);

            return reProfilingConfigurationBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}