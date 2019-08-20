using System;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Providers.Validators;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Policy.Validators
{
    [TestClass]
    public class FundingPeriodValidatorTests
    {
        private FundingPeriod _fundingPeriod;
        private FundingPeriodValidator _validator;

        private ValidationResult _validationResult;

        [TestInitialize]
        public void SetUp()
        {
            _validator = new FundingPeriodValidator();
        }

        [TestMethod]
        [DataRow("")]
        [DataRow(null)]
        [DataRow("  ")]
        public void IsInvalidIfPeriodMissingOrEmpty(string period)
        {
            GivenTheFundingPeriod(_ => _.WithPeriod(period));

            WhenTheFundingPeriodIsValidated();

            ThenTheValidationResultShouldBe(false);
        }

        [TestMethod]
        public void IsOtherwiseValid()
        {
            GivenTheFundingPeriod(_ => _.WithPeriod(new RandomString()));

            WhenTheFundingPeriodIsValidated();

            ThenTheValidationResultShouldBe(true);
        }

        private void ThenTheValidationResultShouldBe(bool expectedIsValidFlag)
        {
            _validationResult
                .IsValid
                .Should()
                .Be(expectedIsValidFlag);
        }

        private void WhenTheFundingPeriodIsValidated()
        {
            _validationResult = _validator.Validate(_fundingPeriod);
        }

        private void GivenTheFundingPeriod(Action<FundingPeriodBuilder> setUp = null)
        {
            FundingPeriodBuilder fundingPeriodBuilder = new FundingPeriodBuilder();

            setUp?.Invoke(fundingPeriodBuilder);

            _fundingPeriod = fundingPeriodBuilder.Build();
        }
    }
}