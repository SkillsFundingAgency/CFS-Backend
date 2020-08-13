using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.FundingPolicy;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Services.Policy.Validators
{
    [TestClass]
    public class SaveFundingDateValidatorTests
    {
        private FundingDate _fundingDate;
        private FundingStream _fundingStream;
        private FundingPeriod _fundingPeriod;

        private ValidationResult _validationResult;

        private IPolicyRepository _policyRepository;
        private SaveFundingDateValidator _validator;

        private string _fundingStreamId;
        private string _fundingPeriodId;

        [TestInitialize]
        public void SetUp()
        {
            _policyRepository = Substitute.For<IPolicyRepository>();

            _validator = new SaveFundingDateValidator(_policyRepository,
                new PolicyResiliencePolicies
                {
                    PolicyRepository = Polly.Policy.NoOpAsync()
                });

            _fundingStreamId = NewRandomString();
            _fundingPeriodId = NewRandomString();
        }

        [TestMethod]
        public void FailsValidationIfNoFundingStreamIdSupplied()
        {
            GivenTheFundingDate();

            WhenTheFundingDateIsValidated();

            ThenTheValidationResultShouldBe(false);
        }

        [TestMethod]
        public void FailsValidationIfFundingStreamNotFound()
        {
            GivenTheFundingDate(_ => _.WithFundingStreamId(_fundingStreamId));

            WhenTheFundingDateIsValidated();

            ThenTheValidationResultShouldBe(false);
        }

        [TestMethod]
        public void FailsValidationIfNoFundingPeriodIdSupplied()
        {
            GivenTheFundingDate(_ => _.WithFundingStreamId(_fundingStreamId));

            GivenTheFundingStream();
            GivenWithGetFundingStreamById();

            WhenTheFundingDateIsValidated();

            ThenTheValidationResultShouldBe(false);
        }

        [TestMethod]
        public void FailsValidationIfFundingPeriodNotFound()
        {
            GivenTheFundingDate(_ => _
                .WithFundingStreamId(_fundingStreamId)
                .WithFundingPeriodId(_fundingPeriodId));

            GivenTheFundingStream();
            GivenWithGetFundingStreamById();

            GivenTheFundingPeriod();

            WhenTheFundingDateIsValidated();

            ThenTheValidationResultShouldBe(false);
        }

        [TestMethod]
        public void FailsValidationIfNoPatternsProvided()
        {
            GivenTheFundingDate(_ => _
                .WithFundingStreamId(_fundingStreamId)
                .WithFundingPeriodId(_fundingPeriodId));

            GivenTheFundingStream();
            GivenWithGetFundingStreamById();

            GivenTheFundingPeriod();
            GivenWithGetFundingPeriodById();

            WhenTheFundingDateIsValidated();

            ThenTheValidationResultShouldBe(false);
        }

        [TestMethod]
        public void FailsValidationIfPatternWithEmptyOccurrenceProvided()
        {
            GivenTheFundingDate(_ => _
                .WithFundingStreamId(_fundingStreamId)
                .WithFundingPeriodId(_fundingPeriodId)
                .WithPatterns(new List<FundingDatePattern> 
                { 
                    GivenTheFundingDatePattern() 
                }));

            GivenTheFundingStream();
            GivenWithGetFundingStreamById();

            GivenTheFundingPeriod();
            GivenWithGetFundingPeriodById();

            WhenTheFundingDateIsValidated();

            ThenTheValidationResultShouldBe(false);
        }

        [TestMethod]
        public void FailsValidationIfDuplicatePatternProvided()
        {
            int occurence = NewRandomInteger();
            string period = NewRandomString();
            int periodYear = NewRandomInteger();
            DateTimeOffset paymentDate = NewRandomDateTimeOffset();

            GivenTheFundingDate(_ => _
                .WithFundingStreamId(_fundingStreamId)
                .WithFundingPeriodId(_fundingPeriodId)
                .WithPatterns(new List<FundingDatePattern>
                {
                    GivenTheFundingDatePattern( fdp => fdp
                        .WithOccurrence(occurence)
                        .WithPeriod(period)
                        .WithPeriodYear(periodYear)
                        .WithPaymentDate(paymentDate)),
                    GivenTheFundingDatePattern( fdp => fdp
                        .WithOccurrence(occurence)
                        .WithPeriod(period)
                        .WithPeriodYear(periodYear)
                        .WithPaymentDate(paymentDate))
                }));

            GivenTheFundingStream();
            GivenWithGetFundingStreamById();

            GivenTheFundingPeriod();
            GivenWithGetFundingPeriodById();

            WhenTheFundingDateIsValidated();

            ThenTheValidationResultShouldBe(false);
        }

        [TestMethod]
        public void SucceedValidationWhenValidFundingDateProvided()
        {
            int occurence = NewRandomInteger();
            string period = NewRandomString();
            int periodYear = NewRandomInteger();
            DateTimeOffset paymentDate = NewRandomDateTimeOffset();

            GivenTheFundingDate(_ => _
                .WithFundingStreamId(_fundingStreamId)
                .WithFundingPeriodId(_fundingPeriodId)
                .WithPatterns(new List<FundingDatePattern>
                {
                    GivenTheFundingDatePattern( fdp => fdp
                        .WithOccurrence(occurence)
                        .WithPeriod(period)
                        .WithPeriodYear(periodYear)
                        .WithPaymentDate(paymentDate))
                }));

            GivenTheFundingStream();
            GivenWithGetFundingStreamById();

            GivenTheFundingPeriod();
            GivenWithGetFundingPeriodById();

            WhenTheFundingDateIsValidated();

            ThenTheValidationResultShouldBe(true);
        }

        private void GivenTheFundingDate(Action<FundingDateBuilder> setUp = null)
        {
            FundingDateBuilder fundingDateBuilder = new FundingDateBuilder();

            setUp?.Invoke(fundingDateBuilder);

            _fundingDate = fundingDateBuilder.Build();
        }

        private void GivenTheFundingStream(Action<FundingStreamBuilder> setUp = null)
        {
            FundingStreamBuilder fundingStreamBuilder = new FundingStreamBuilder();

            setUp?.Invoke(fundingStreamBuilder);

            _fundingStream = fundingStreamBuilder.Build();
        }

        private void GivenTheFundingPeriod(Action<FundingPeriodBuilder> setUp = null)
        {
            FundingPeriodBuilder fundingPeriodBuilder = new FundingPeriodBuilder();

            setUp?.Invoke(fundingPeriodBuilder);

            _fundingPeriod = fundingPeriodBuilder.Build();
        }

        private FundingDatePattern GivenTheFundingDatePattern(Action<FundingDatePatternBuilder> setUp = null)
        {
            FundingDatePatternBuilder fundingDatePatternBuilder = new FundingDatePatternBuilder();

            setUp?.Invoke(fundingDatePatternBuilder);

            return fundingDatePatternBuilder.Build();
        }

        private void GivenWithGetFundingStreamById()
        {
            _policyRepository
                .GetFundingStreamById(_fundingStreamId)
                .Returns(_fundingStream);
        }

        private void GivenWithGetFundingPeriodById()
        {
            _policyRepository
                .GetFundingPeriodById(_fundingPeriodId)
                .Returns(_fundingPeriod);
        }

        private void WhenTheFundingDateIsValidated()
        {
            _validationResult = _validator.Validate(_fundingDate);
        }

        private void ThenTheValidationResultShouldBe(bool expectedIsValidFlag)
        {
            _validationResult
                .IsValid
                .Should()
                .Be(expectedIsValidFlag);
        }

        public static IEnumerable<object[]> FlagExamples()
        {
            yield return new object[] { true };
            yield return new object[] { false };
        }

        private string NewRandomString() => new RandomString();
        private int NewRandomInteger() => new RandomNumberBetween(0, 100);
        private DateTimeOffset NewRandomDateTimeOffset() => new DateTimeOffset(new RandomDateTime());
    }
}
