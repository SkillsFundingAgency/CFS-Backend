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
            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithFundingLines(
                NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment)
                    .WithValue(999)
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(
                            NewProfilePeriod(pp =>
                                pp.WithAmount(10))))))))));

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider);

            publishedProvider.Current
                .Errors
                .Should()
                .BeNullOrEmpty();
        }
        
        [TestMethod]
        public async Task TakesCarryOversIntoAccountWhenCheckingValueMismatchErrorForEachPaymentFundingLineWithValueMismatchesWhereWeHaveACustomProfile()
        {
            string fundingLineCode1 = NewRandomString();

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithProfilePatternKeys(
                    NewProfilePatternKey(pk => pk.WithFundingLineCode(fundingLineCode1)))
                .WithCarryOvers(NewProfilingCarryOver(pco => pco.WithAmount(989)
                    .WithFundingLineCode(fundingLineCode1)
                    .WithType(ProfilingCarryOverType.CustomProfile)))
                .WithFundingLines(
                NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment)
                    .WithFundingLineCode(fundingLineCode1)
                    .WithValue(999)
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(
                            NewProfilePeriod(pp =>
                                pp.WithAmount(10))))))))));

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider);

            publishedProvider.Current
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

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithProfilePatternKeys(
                    NewProfilePatternKey(pk => pk.WithFundingLineCode(fundingLineCode1)))
            .WithFundingStreamId("fs1")
                .WithFundingLines(NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment)
                    .WithName("fl1")
                    .WithFundingLineCode(fundingLineCode1)
                    .WithValue(999)
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(
                            NewProfilePeriod(pp =>
                                pp.WithAmount(10)))))),
                NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment)
                    .WithFundingLineCode(fundingLineCode2)
                    .WithValue(999)
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(
                            NewProfilePeriod(pp =>
                                pp.WithAmount(999)))))),
                NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment)
                    .WithFundingLineCode(fundingLineCode3)
                    .WithValue(666)
                    .WithDistributionPeriods(NewDistributionPeriod(dp =>
                        dp.WithProfilePeriods(
                            NewProfilePeriod(pp =>
                                pp.WithAmount(999))))))))));

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider);

            publishedProvider.Current
                .Errors
                .Should()
                .NotBeNullOrEmpty();

            AndPublishedProviderShouldHaveTheErrors(publishedProvider.Current,
                NewError(_ => _.WithFundingLineCode(fundingLineCode1)
                    .WithType(PublishedProviderErrorType.FundingLineValueProfileMismatch)
                    .WithSummaryErrorMessage("A funding line profile doesn't match allocation value.")
                    .WithDetailedErrorMessage("Funding line profile doesn't match allocation value. The allocation value is £999, but the profile value is set to £10")
                    .WithFundingStreamId("fs1")
                    .WithFundingLine(fundingLineCode1)));
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
                    .SingleOrDefault(_ => _.Identifier == expectedError.Identifier);

                actualError
                    .Should()
                    .BeEquivalentTo(expectedError);
            }
        }

        private async Task WhenErrorsAreDetectedOnThePublishedProvider(PublishedProvider publishedProvider)
        {
            await _errorDetector.DetectErrors(publishedProvider, null);
        }

        private static PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
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

        private ProfilingCarryOver NewProfilingCarryOver(Action<ProfilingCarryOverBuilder> setUp = null)
        {
            ProfilingCarryOverBuilder profilingCarryOverBuilder = new ProfilingCarryOverBuilder();

            setUp?.Invoke(profilingCarryOverBuilder);
            
            return profilingCarryOverBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
    }
}