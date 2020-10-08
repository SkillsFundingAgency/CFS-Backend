using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Profiling.Custom;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling.Overrides
{
    [TestClass]
    public class ApplyCustomProfileRequestValidatorTests : CustomProfileRequestTestBase
    {
        private Mock<IPublishedFundingRepository> _publishedFunding;
        private string _fundingLineCode;
        private string _providerId;
        private string _fundingStreamId;
        private string _fundingPeriodId;

        private ValidationResult _result;

        private ApplyCustomProfileRequestValidator _validator;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFunding = new Mock<IPublishedFundingRepository>();

            _validator = new ApplyCustomProfileRequestValidator(_publishedFunding.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync()
                });

            _fundingLineCode = NewRandomString();
            _fundingPeriodId = NewRandomString();
            _fundingStreamId = NewRandomString();
            _providerId = NewRandomString();
        }

        [TestMethod]
        public async Task FailsValidationIfNoProviderIdOnRequest()
        {
            GivenThePublishedProvider(NewOtherwiseValidPublishedProvider());

            await WhenTheRequestIsValidated(NewOtherwiseValidRequest(_ => _.ProviderId = null));

            ThenTheValidationResultsContainsTheErrors(("ProviderId", "You must supply a provider id"));
        }

        [TestMethod]
        public async Task FailsValidationIfNoFundingStreamIdOnRequest()
        {
            GivenThePublishedProvider(NewOtherwiseValidPublishedProvider());

            await WhenTheRequestIsValidated(NewOtherwiseValidRequest(_ => _.FundingStreamId = null));

            ThenTheValidationResultsContainsTheErrors(("FundingStreamId", "You must supply a funding stream id"));
        }

        [TestMethod]
        public async Task FailsValidationIfNoFundingPeriodIdOnRequest()
        {
            GivenThePublishedProvider(NewOtherwiseValidPublishedProvider());

            await WhenTheRequestIsValidated(NewOtherwiseValidRequest(_ => _.FundingPeriodId = null));

            ThenTheValidationResultsContainsTheErrors(("FundingPeriodId", "You must supply a funding period id"));
        }

        [TestMethod]
        public async Task FailsValidationIfNoFundingLineCodeOnRequest()
        {
            GivenThePublishedProvider(NewOtherwiseValidPublishedProvider());

            await WhenTheRequestIsValidated(NewOtherwiseValidRequest(_ => _.FundingLineCode = null));

            ThenTheValidationResultsContainsTheErrors(("FundingLineCode", "You must supply a funding line code"));
        }

        [TestMethod]
        public async Task FailsValidationIfNoCustomProfileNameOnRequest()
        {
            GivenThePublishedProvider(NewOtherwiseValidPublishedProvider());

            await WhenTheRequestIsValidated(NewOtherwiseValidRequest(_ => _.CustomProfileName = null));

            ThenTheValidationResultsContainsTheErrors(("CustomProfileName", "You must supply a custom profile name"));
        }

        [TestMethod]
        public async Task FailsValidationIfNoProfilePeriodsOnRequest()
        {
            GivenThePublishedProvider(NewOtherwiseValidPublishedProvider());

            await WhenTheRequestIsValidated(NewOtherwiseValidRequest(_ => _.ProfilePeriods = null));

            ThenTheValidationResultsContainsTheErrors(("ProfilePeriods", "You must supply at least one profile period"));
        }

        [TestMethod]
        public async Task FailsValidationIfNoPublishedProviderMatchingTheRequest()
        {
            GivenThePublishedProvider(NewOtherwiseValidPublishedProvider());

            await WhenTheRequestIsValidated(NewOtherwiseValidRequest(_ => _.ProviderId = NewRandomString()));

            ThenTheValidationResultsContainsTheErrors(("Request", "No matching published provider located"));
        }

        [TestMethod]
        public async Task FailsValidationIfTheProfilePeriodsHaveDuplicateOccurrencesInAFundingLine()
        {
            GivenThePublishedProvider(NewOtherwiseValidPublishedProvider());

            await WhenTheRequestIsValidated(NewOtherwiseValidRequest(_ => _.ProfilePeriods =
                NewProfilePeriods(
                    NewProfilePeriod(pp =>
                        pp.WithOccurence(1)
                            .WithYear(2020)
                            .WithTypeValue("January")
                            .WithDistributionPeriodId("FY-2021")
                            .WithType(ProfilePeriodType.CalendarMonth)),
                    NewProfilePeriod(pp =>
                        pp.WithOccurence(1)
                            .WithYear(2020)
                            .WithTypeValue("January")
                            .WithDistributionPeriodId("FY-2021")
                            .WithType(ProfilePeriodType.CalendarMonth)))));

            ThenTheValidationResultsContainsTheErrors(("ProfilePeriods", "The profile periods must be for unique occurrences in a funding line"));
        }

        [TestMethod]
        public async Task FailsValidationIfTheProfilePeriodsHaveMoreThanOneDistinctDistributionPeriodInRequest()
        {
            GivenThePublishedProvider(NewOtherwiseValidPublishedProvider());

            await WhenTheRequestIsValidated(NewOtherwiseValidRequest(_ => _.ProfilePeriods =
                NewProfilePeriods(
                    NewProfilePeriod(pp =>
                        pp.WithOccurence(1)
                            .WithYear(2020)
                            .WithTypeValue("January")
                            .WithDistributionPeriodId("FY-2021")
                            .WithType(ProfilePeriodType.CalendarMonth)),
                    NewProfilePeriod(pp =>
                        pp.WithOccurence(1)
                            .WithYear(2020)
                            .WithTypeValue("January")
                            .WithDistributionPeriodId("FY-2022")
                            .WithType(ProfilePeriodType.CalendarMonth)))));

            ThenTheValidationResultsContainsTheErrors(("ProfilePeriods", "The profile periods must be specified for one distribution period only"));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        public async Task FailsValidationIfDistributionIdIsNotSet(string distributionId)
        {
            GivenThePublishedProvider(NewOtherwiseValidPublishedProvider());

            ApplyCustomProfileRequest request = NewOtherwiseValidRequest(_ => _.ProfilePeriods =
                NewProfilePeriods(
                    NewProfilePeriod(pp =>
                        pp.WithOccurence(1)
                            .WithYear(2020)
                            .WithTypeValue("January")
                            .WithType(ProfilePeriodType.CalendarMonth)),
                    NewProfilePeriod(pp =>
                        pp.WithOccurence(1)
                            .WithYear(2020)
                            .WithTypeValue("February")
                            .WithType(ProfilePeriodType.CalendarMonth))));

            request.ProfilePeriods.First().DistributionPeriodId = distributionId;

            await WhenTheRequestIsValidated(request);

            ThenTheValidationResultsContainsTheErrors(("ProfilePeriods", "The distribution id must be supplied for all profile periods"));
        }

        [TestMethod]
        public async Task OtherwisePassesValidation()
        {
            GivenThePublishedProvider(NewOtherwiseValidPublishedProvider());

            await WhenTheRequestIsValidated(NewOtherwiseValidRequest());

            ThenThereAreNoValidationErrors();
        }

        private void GivenThePublishedProvider(PublishedProvider publishedProvider)
        {
            string key = publishedProvider.Id;

            _publishedFunding.Setup(_ => _.GetPublishedProviderById(key, key))
                .ReturnsAsync(publishedProvider);
        }

        private async Task WhenTheRequestIsValidated(ApplyCustomProfileRequest request)
        {
            _result = await _validator.ValidateAsync(request);
        }

        private void ThenThereAreNoValidationErrors()
        {
            _result
                .IsValid
                .Should()
                .BeTrue();
        }

        private void ThenTheValidationResultsContainsTheErrors(params (string, string)[] errors)
        {
            _result.Errors.Count
                .Should()
                .Be(errors.Length);

            foreach ((string, string) error in errors)
            {
                _result.Errors
                    .Should()
                    .Contain(_ => _.PropertyName == error.Item1 &&
                                  _.ErrorMessage == error.Item2,
                        $"Expected validation errors to contain {error.Item1}:{error.Item2}");
            }
        }

        private ApplyCustomProfileRequest NewOtherwiseValidRequest(Action<ApplyCustomProfileRequest> overrides = null)
        {
            ApplyCustomProfileRequest request = NewApplyCustomProfileRequest(_ => _
                .WithProviderId(_providerId)
                .WithFundingStreamId(_fundingStreamId)
                .WithFundingLineCode(_fundingLineCode)
                .WithProfilePeriods(NewProfilePeriod(pp =>
                    pp.WithOccurence(1)
                        .WithYear(2020)
                        .WithTypeValue("January")
                        .WithType(ProfilePeriodType.CalendarMonth)))
                .WithFundingPeriodId(_fundingPeriodId));

            overrides?.Invoke(request);

            return request;
        }

        private PublishedProvider NewOtherwiseValidPublishedProvider(Action<PublishedProvider> overrides = null)
        {
            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(ppv =>
                    ppv.WithProviderId(_providerId)
                        .WithFundingStreamId(_fundingStreamId)
                        .WithFundingPeriodId(_fundingPeriodId)
                        .WithFundingLines(NewFundingLine(fl =>
                            fl.WithFundingLineCode(_fundingLineCode)
                                .WithDistributionPeriods())))));

            overrides?.Invoke(publishedProvider);

            return publishedProvider;
        }
    }
}