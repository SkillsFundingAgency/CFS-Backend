using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;

namespace CalculateFunding.Services.Profiling.Tests
{
    [TestClass]
    public class CreateProfilePatternValidatorTests : ProfileRequestValidatorTest
    {
        private CreateProfilePatternValidator _validator;

        [TestInitialize]
        public void SetUp()
        {
            _validator = new CreateProfilePatternValidator(ProfilePatterns.Object,
                new ProfilingResiliencePolicies
                {
                    ProfilePatternRepository = Policy.NoOpAsync()
                });
        }

        [TestMethod]
        public async Task FailsValidationIfNoPatternOnRequest()
        {
            await WhenTheRequestIsValidated(NewCreateRequest());

            ThenTheValidationResultsContainsTheErrors(("Pattern", "'Pattern' must not be empty."));
        }

        [TestMethod]
        public async Task FailsValidationIfPatternWithSameKeyExists()
        {
            CreateProfilePatternRequest request = NewCreateRequest(_ => _.WithPattern(NewProfilePattern()));
            FundingStreamPeriodProfilePattern pattern = request.Pattern;

            GivenTheExistingProfilePattern(pattern);

            await WhenTheRequestIsValidated(request);

            ThenTheValidationResultsContainsTheErrors(("Id", $"{pattern.Id} is already in use. Please choose a unique profile pattern id"));
        }

        [TestMethod]
        public async Task PassesValidationWhenPatternSuppliedAndKeyIsUnique()
        {
            await WhenTheRequestIsValidated(NewCreateRequest(_ => _.WithPattern(NewProfilePattern())));

            ThenThereAreNoValidationErrors();
        }

        [TestMethod]
        public async Task FailsValidationIfNoFundingStreamIdOnPattern()
        {
            CreateProfilePatternRequest request = NewOtherwiseValidCreateRequest(_ => _.FundingStreamId = null);

            await WhenTheRequestIsValidated(request);

            ThenTheValidationResultsContainsTheErrors(("Pattern.FundingStreamId", "You must provide a funding stream id"));
        }

        [TestMethod]
        public async Task FailsValidationIfNoFundingPeriodIdOnPattern()
        {
            CreateProfilePatternRequest request = NewOtherwiseValidCreateRequest(_ => _.FundingPeriodId = null);

            await WhenTheRequestIsValidated(request);

            ThenTheValidationResultsContainsTheErrors(("Pattern.FundingPeriodId", "You must provide a funding period id"));
        }

        [TestMethod]
        public async Task FailsValidationIfNoFundingLineIdOnPattern()
        {
            CreateProfilePatternRequest request = NewOtherwiseValidCreateRequest(_ => _.FundingLineId = null);

            await WhenTheRequestIsValidated(request);

            ThenTheValidationResultsContainsTheErrors(("Pattern.FundingLineId", "You must provide a funding line id"));
        }

        [TestMethod]
        public async Task FailsValidationIfNoStartDateOnPattern()
        {
            CreateProfilePatternRequest request = NewOtherwiseValidCreateRequest(_ => _.FundingStreamPeriodStartDate = DateTime.MinValue);

            await WhenTheRequestIsValidated(request);

            ThenTheValidationResultsContainsTheErrors(("Pattern.FundingStreamPeriodStartDate", "You must provide a funding stream period start date"));
        }

        [TestMethod]
        public async Task FailsValidationIfEndDateOnPatternNotAfterStartDate()
        {
            CreateProfilePatternRequest request = NewOtherwiseValidCreateRequest(_ => _.FundingStreamPeriodEndDate = DateTime.MinValue);

            await WhenTheRequestIsValidated(request);

            ThenTheValidationResultsContainsTheErrors(("Pattern.FundingStreamPeriodEndDate", "Funding stream period end date must after funding stream period start date"));
        }

        [TestMethod]
        public async Task FailsValidationIfNoProfilePeriodsInPattern()
        {
            CreateProfilePatternRequest request = NewOtherwiseValidCreateRequest(_ => _.ProfilePattern = null);

            await WhenTheRequestIsValidated(request);

            ThenTheValidationResultsContainsTheErrors(("ProfilePattern", "The profile pattern must have at least one period"));
        }

        [TestMethod]
        public async Task FailsValidationIfProfilePeriodsInPatternForUniquePeriods()
        {
            CreateProfilePatternRequest request = NewOtherwiseValidCreateRequest(_ => _.ProfilePattern = NewPeriods(
                NewPeriod(p => p.WithOccurrence(1)
                    .WithPercentage(50)
                    .WithPeriod("January")
                    .WithType(PeriodType.CalendarMonth)
                    .WithYear(2020)),
                NewPeriod(p => p.WithOccurrence(1)
                    .WithPercentage(50)
                    .WithPeriod("January")
                    .WithType(PeriodType.CalendarMonth)
                    .WithYear(2020))));

            await WhenTheRequestIsValidated(request);

            ThenTheValidationResultsContainsTheErrors(("ProfilePattern", "The profile periods must be for unique dates and occurence"));
        }

        [TestMethod]
        public async Task FailsValidationIfProfilePeriodsPercentagesDoNotTotal100()
        {
            CreateProfilePatternRequest request = NewOtherwiseValidCreateRequest(_ => _.ProfilePattern = NewPeriods(
                NewPeriod(p => p.WithOccurrence(0)
                    .WithPercentage(50)
                    .WithPeriod("January")
                    .WithType(PeriodType.CalendarMonth)
                    .WithYear(2020)),
                NewPeriod(p => p.WithOccurrence(1)
                    .WithPercentage(51)
                    .WithPeriod("January")
                    .WithType(PeriodType.CalendarMonth)
                    .WithYear(2020))));

            await WhenTheRequestIsValidated(request);

            ThenTheValidationResultsContainsTheErrors(("ProfilePattern", "The profile period percentages must total 100%"));
        }

        [TestMethod]
        public async Task FailValidationIfNoDisplayName()
        {
            await WhenTheRequestIsValidated(NewCreateRequest(_ => _.WithPattern(NewProfilePattern(p => p.WithProfilePatternDisplayName(string.Empty)))));

            ThenTheValidationResultsContainsTheErrors(("Pattern.ProfilePatternDisplayName", "Null or Empty profile pattern display name provided"));
        }

        [TestMethod]
        public async Task FailValidationIfDefaultPatternHaveProviderTypeSubTypes()
        {
            await WhenTheRequestIsValidated(NewCreateRequest(_ => _.WithPattern(NewProfilePattern(p => p.WithProfilePatternKey(string.Empty).WithProviderTypeSubTypes(new[] { new ProviderTypeSubType { ProviderType = NewRandomString(), ProviderSubType = NewRandomString() } })))));

            ThenTheValidationResultsContainsTheErrors(("ProfilePatternKey", "Default pattern not allowed to have ProviderTypeSubTypes"));
        }

        private async Task WhenTheRequestIsValidated(CreateProfilePatternRequest request)
        {
            Result = await _validator.ValidateAsync(request);
        }

        private CreateProfilePatternRequest NewCreateRequest(Action<CreateProfilePatternRequestBuilder> setUp = null)
        {
            CreateProfilePatternRequestBuilder builder = new CreateProfilePatternRequestBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private CreateProfilePatternRequest NewOtherwiseValidCreateRequest(Action<FundingStreamPeriodProfilePattern> overrides)
        {
            CreateProfilePatternRequest request = NewCreateRequest(_ => _.WithPattern(NewProfilePattern()));

            overrides(request.Pattern);

            return request;
        }
    }
}