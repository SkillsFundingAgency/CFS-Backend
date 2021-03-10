using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Errors;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Errors
{
    [TestClass]
    public class ProfilingConsistencyCheckErrorDetectorTests : FundingLineErrorDetectorTest
    {
        private ProfilingConsistencyCheckErrorDetector _errorDetector;

        [TestInitialize]
        public void SetUp()
        {
            _errorDetector = new ProfilingConsistencyCheckErrorDetector();
        }

        [TestMethod]
        public void RunsForAllFundingConfigurationsPostVariations()
        {
            _errorDetector.IsPostVariationCheck
                .Should()
                .BeTrue();

            _errorDetector.IsPreVariationCheck
                .Should()
                .BeFalse();

            _errorDetector.IsForAllFundingConfigurations
                .Should()
                .BeTrue();

            _errorDetector.IsAssignProfilePatternCheck
                .Should()
                .BeFalse();
        }

        [TestMethod]
        public async Task TreatsNullValueFundingLinesWithDistributionPeriodsAsError()
        {
            string fundingLineCode1 = NewRandomString();
            string fundingLineCode2 = NewRandomString();

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv
                .WithFundingStreamId("fs1")
                .WithFundingLines(NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment)
                        .WithValue(null)
                        .WithFundingLineCode(fundingLineCode1)
                        .WithDistributionPeriods(NewDistributionPeriod(dp =>
                            dp.WithProfilePeriods(
                                NewProfilePeriod(pp =>
                                    pp.WithAmount(10)))))),
                    NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment)
                        .WithFundingLineCode(fundingLineCode2)
                        .WithValue(null))))));

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider);

            publishedProvider.Current
                .Errors
                .Should()
                .NotBeNullOrEmpty();

            string errorMessage = $"Post Profiling and Variations - The payment funding line {fundingLineCode1} has a null total" +
                                  " but contains 1 distributions periods";

            AndPublishedProviderShouldHaveTheErrors(publishedProvider.Current,
                NewError(_ => _.WithFundingLineCode(fundingLineCode1)
                    .WithType(PublishedProviderErrorType.ProfilingConsistencyCheckFailure)
                    .WithSummaryErrorMessage(errorMessage)
                    .WithDetailedErrorMessage(errorMessage)
                    .WithFundingStreamId("fs1")
                    .WithFundingLine(fundingLineCode1)));
        }

        [TestMethod]
        public async Task TreatsDistributionPeriodsTotalNotEquallingFundingLineTotalAsError()
        {
            string fundingLineCode1 = NewRandomString();
            string fundingLineCode2 = NewRandomString();

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv
                .WithFundingStreamId("fs1")
                .WithFundingLines(NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment)
                        .WithValue(200M)
                        .WithFundingLineCode(fundingLineCode1)
                        .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                dp.WithValue(100M)
                                    .WithProfilePeriods(NewProfilePeriod(pp => pp.WithAmount(100M)))),
                            NewDistributionPeriod(dp =>
                                dp.WithValue(101M)
                                    .WithProfilePeriods(NewProfilePeriod(pp => pp.WithAmount(101M)))))),
                    NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment)
                        .WithValue(200M)
                        .WithFundingLineCode(fundingLineCode2)
                        .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                dp.WithValue(100M)
                                    .WithProfilePeriods(NewProfilePeriod(pp => pp.WithAmount(100M)))),
                            NewDistributionPeriod(dp =>
                                dp.WithValue(100M)
                                    .WithProfilePeriods(NewProfilePeriod(pp => pp.WithAmount(100M))))))))));

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider);

            publishedProvider.Current
                .Errors
                .Should()
                .NotBeNullOrEmpty();

            string errorMessage = $"Post Profiling and Variations - The payment funding line {fundingLineCode1} has a total expected" +
                                  " funding of 200 but the distribution periods total for the funding line is 201";

            AndPublishedProviderShouldHaveTheErrors(publishedProvider.Current,
                NewError(_ => _.WithFundingLineCode(fundingLineCode1)
                    .WithType(PublishedProviderErrorType.ProfilingConsistencyCheckFailure)
                    .WithSummaryErrorMessage(errorMessage)
                    .WithDetailedErrorMessage(errorMessage)
                    .WithFundingStreamId("fs1")
                    .WithFundingLine(fundingLineCode1)));
        }

        [TestMethod]
        public async Task TreatsDistributionPeriodTotalNotEquallingProfilePeriodsTotalAsError()
        {
            string fundingLineCode1 = NewRandomString();
            string fundingLineCode2 = NewRandomString();
            string distributionPeriod1 = NewRandomString();
            string distributionPeriod2 = NewRandomString();

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv
                .WithFundingStreamId("fs1")
                .WithFundingLines(NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment)
                        .WithValue(201M)
                        .WithFundingLineCode(fundingLineCode1)
                        .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                dp.WithValue(100M)
                                    .WithDistributionPeriodId(distributionPeriod1)
                                    .WithProfilePeriods(NewProfilePeriod(pp => pp.WithAmount(100M)))),
                            NewDistributionPeriod(dp =>
                                dp.WithValue(101M)
                                    .WithDistributionPeriodId(distributionPeriod2)
                                    .WithProfilePeriods(NewProfilePeriod(pp => pp.WithAmount(101M)))))),
                    NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment)
                        .WithValue(200M)
                        .WithFundingLineCode(fundingLineCode2)
                        .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                dp.WithValue(100M)
                                    .WithDistributionPeriodId(distributionPeriod1)
                                    .WithProfilePeriods(NewProfilePeriod(pp => pp.WithAmount(100M)))),
                            NewDistributionPeriod(dp =>
                                dp.WithValue(100M)
                                    .WithDistributionPeriodId(distributionPeriod2)
                                    .WithProfilePeriods(NewProfilePeriod(pp => pp.WithAmount(101M))))))))));

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider);

            publishedProvider.Current
                .Errors
                .Should()
                .NotBeNullOrEmpty();

            string errorMessage = $"Post Profiling and Variations - The payment funding line {fundingLineCode2} distribution period {distributionPeriod2} " +
                                  "has a total expected funding of 100 but the total profiled for the distribution period is 101";

            AndPublishedProviderShouldHaveTheErrors(publishedProvider.Current,
                NewError(_ => _.WithFundingLineCode(fundingLineCode2)
                    .WithType(PublishedProviderErrorType.ProfilingConsistencyCheckFailure)
                    .WithSummaryErrorMessage(errorMessage)
                    .WithDetailedErrorMessage(errorMessage)
                    .WithFundingStreamId("fs1")
                    .WithFundingLine(fundingLineCode2)));
        }

        [TestMethod]
        public async Task IgnoresFundingLinesWithCustomProfiles()
        {
            string fundingLineCode1 = NewRandomString();
            string fundingLineCode2 = NewRandomString();

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv
                .WithProfilePatternKeys(NewProfilePatternKey(ppk => ppk.WithFundingLineCode(fundingLineCode1)))
                .WithFundingStreamId("fs1")
                .WithFundingLines(NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment)
                        .WithValue(null)
                        .WithFundingLineCode(fundingLineCode1)
                        .WithDistributionPeriods(NewDistributionPeriod(dp =>
                            dp.WithProfilePeriods(
                                NewProfilePeriod(pp =>
                                    pp.WithAmount(10)))))),
                    NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment)
                        .WithFundingLineCode(fundingLineCode2)
                        .WithValue(null))))));

            await WhenErrorsAreDetectedOnThePublishedProvider(publishedProvider);

            publishedProvider.Current
                .Errors
                .Should()
                .BeNullOrEmpty();
        }

        private async Task WhenErrorsAreDetectedOnThePublishedProvider(PublishedProvider publishedProvider)
        {
            await _errorDetector.DetectErrors(publishedProvider, null);
        }
    }
}