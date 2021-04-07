using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Errors;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Errors
{
    [TestClass]
    public class FundingLineValueProfileMismatchErrorDetectorTests : PublishedProviderErrorDetectorTest
    {
        private FundingLineValueProfileMismatchErrorDetector _errorDetector;

        [TestInitialize]
        public void SetUp()
        {
            _errorDetector = new FundingLineValueProfileMismatchErrorDetector();
        }

        [TestMethod]
        public void RunsForPreVariationsAndAssignProfilePatternCheck()
        {
            _errorDetector.IsPostVariationCheck
                .Should()
                .BeFalse();

            _errorDetector.IsPreVariationCheck
                .Should()
                .BeTrue();

            _errorDetector.IsForAllFundingConfigurations
                .Should()
                .BeFalse();

            _errorDetector.IsAssignProfilePatternCheck
                .Should()
                .BeTrue();
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
                NewError(_ => _.WithIdentifier(fundingLineCode1)
                    .WithType(PublishedProviderErrorType.FundingLineValueProfileMismatch)
                    .WithSummaryErrorMessage("A funding line profile doesn't match allocation value.")
                    .WithDetailedErrorMessage("Funding line profile doesn't match allocation value. The allocation value is £999, but the profile value is set to £10")
                    .WithFundingStreamId("fs1")
                    .WithFundingLine(fundingLineCode1)));
        }

        private async Task WhenErrorsAreDetectedOnThePublishedProvider(PublishedProvider publishedProvider)
        {
            await _errorDetector.DetectErrors(publishedProvider, null);
        }
    }
}