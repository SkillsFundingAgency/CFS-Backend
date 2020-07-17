using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Errors;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Errors
{
    [TestClass]
    public class FundingLineValueProfileMismatchErrorDetectorTests
    {
        private FundingLineValueProfileMismatchErrorDetector _errorDetector;

        [TestInitialize]
        public void SetUp()
        {
            _errorDetector = new FundingLineValueProfileMismatchErrorDetector();
        }

        [TestMethod]
        public async Task OnlyChecksProfiledValuesIfTheProviderHasAProfilePatternKey()
        {
            PublishedProviderVersion publishedProvider = NewPublishedProviderVersion(_ => _.WithFundingLines(
                NewFundingLine(fl => fl.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                    .WithValue(999)
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(
                            NewProfilePeriod(pp =>
                                pp.WithAmount(10))))))));

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider);

            publishedProvider
                .Errors
                .Should()
                .BeNullOrEmpty();
        }

        [TestMethod]
        public async Task AddsProfiledValueMismatchErrorForEachPaymentFundingLineWithValueMismatchesWhereWeHaveACustomProfile()
        {
            string fundingLineCode1 = NewRandomString();
            string fundingLineCode2 = NewRandomString();
            string fundingLineCode3 = NewRandomString();

            PublishedProviderVersion publishedProvider = NewPublishedProviderVersion(_ => _.WithProfilePatternKeys(
                    NewProfilePatternKey(pk => pk.WithFundingLineCode(fundingLineCode1)))
                .WithFundingLines(
                NewFundingLine(fl => fl.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                    .WithFundingLineCode(fundingLineCode1)
                    .WithValue(999)
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(
                            NewProfilePeriod(pp =>
                                pp.WithAmount(10)))))),
                NewFundingLine(fl => fl.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                    .WithFundingLineCode(fundingLineCode2)
                    .WithValue(999)
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(
                            NewProfilePeriod(pp =>
                                pp.WithAmount(999)))))),
                NewFundingLine(fl => fl.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                    .WithFundingLineCode(fundingLineCode3)
                    .WithValue(666)
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(
                            NewProfilePeriod(pp =>
                                pp.WithAmount(999))))))));

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider);

            publishedProvider
                .Errors
                .Should()
                .NotBeNullOrEmpty();

            AndPublishedProviderShouldHaveTheErrors(publishedProvider,
                NewError(_ => _.WithFundingLineCode(fundingLineCode1)
                    .WithType(PublishedProviderErrorType.FundingLineValueProfileMismatch)
                    .WithDescription("Expected total funding line to be 999 but custom profiles total 10")));

        }

        private void AndPublishedProviderShouldHaveTheErrors(PublishedProviderVersion providerVersion,
            params PublishedProviderError[] expectedErrors)
        {
            providerVersion.Errors
                .Count
                .Should()
                .Be(expectedErrors.Length);

            foreach (PublishedProviderError expectedError in expectedErrors)
            {
                PublishedProviderError actualError = providerVersion.Errors
                    .SingleOrDefault(_ => _.FundingLineCode == expectedError.FundingLineCode);

                actualError
                    .Should()
                    .BeEquivalentTo(expectedError);
            }
        }

        private async Task WhenErrorsAreDetectedOnThePublishedProvider(PublishedProviderVersion publishedProviderVersion)
        {
            await _errorDetector.DetectErrors(publishedProviderVersion);
        }

        private static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        private static FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        private static DistributionPeriod NewDistributionPeriod(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder periodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(periodBuilder);

            return periodBuilder.Build();
        }

        private static ProfilePeriod NewProfilePeriod(Action<ProfilePeriodBuilder> setUp = null)
        {
            ProfilePeriodBuilder periodBuilder = new ProfilePeriodBuilder();

            setUp?.Invoke(periodBuilder);

            return periodBuilder.Build();
        }

        private static PublishedProviderError NewError(Action<PublishedProviderErrorBuilder> setUp = null)
        {
            PublishedProviderErrorBuilder providerErrorBuilder = new PublishedProviderErrorBuilder();

            setUp?.Invoke(providerErrorBuilder);

            return providerErrorBuilder.Build();
        }

        private ProfilePatternKey NewProfilePatternKey(Action<ProfilePatternKeyBuilder> setUp = null)
        {
            ProfilePatternKeyBuilder patternKeyBuilder = new ProfilePatternKeyBuilder();

            setUp?.Invoke(patternKeyBuilder);

            return patternKeyBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}