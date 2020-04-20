using System;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.UnitTests.Profiling.Overrides;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class ReApplyCustomProfilesTests
    {
        private ReApplyCustomProfiles _reApplyCustomProfiles;
        private Mock<IPublishedProviderErrorDetection> _errorDetection;

        [TestInitialize]
        public void SetUp()
        {
            _errorDetection = new Mock<IPublishedProviderErrorDetection>();
            
            _reApplyCustomProfiles = new ReApplyCustomProfiles(_errorDetection.Object);     
        }

        [TestMethod]
        public void GuardsAgainstNoPublishedProviderVersionBeingSupplied()
        {
            Func<Task> invocation = () => WhenThePublishedProviderIsProcessed(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("publishedProviderVersion");
        }

        [TestMethod]
        public void ThrowsExceptionIfCustomProfileReferencesFundingLineNoLongerInThePublishedProvider()
        {
            string missingFundingLineCode = NewRandomString();
            
            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ =>
                _.WithCustomProfiles(NewFundingLineOverrides(fl => 
                    fl.WithFundingLineCode(missingFundingLineCode))));

            Func<Task> invocation = () => WhenThePublishedProviderIsProcessed(publishedProviderVersion);

            invocation
                .Should()
                .Throw<InvalidOperationException>()
                .Which
                .Message
                .Should()
                .Be($"Custom profile has no matching funding line for {missingFundingLineCode} on {publishedProviderVersion.Id}");
        }

        [TestMethod]
        public async Task OverridesDistributionPeriodsOnFundingLinesWhereThereIsACustomProfileOnThePublishedProviderVersionAndChecksForErrors()
        {
            string fundingLineWithCustomProfile = NewRandomString();
            string fundingLineCodeTwo = NewRandomString();

            DistributionPeriod customProfile = NewDistributionPeriod(dp =>
                dp.WithProfilePeriods(NewProfilePeriod(), NewProfilePeriod()));

            DistributionPeriod profiledDistributionPeriod = NewDistributionPeriod(dp =>
                dp.WithProfilePeriods(NewProfilePeriod()));

            FundingLine fundingLineForCustomProfile = NewFundingLine(fl => fl.WithFundingLineCode(fundingLineWithCustomProfile)
                .WithDistributionPeriods(NewDistributionPeriod(dp =>
                    dp.WithProfilePeriods(NewProfilePeriod()))));
            FundingLine fundingLineWithProfiling = NewFundingLine(fl => fl.WithFundingLineCode(fundingLineCodeTwo)
                .WithDistributionPeriods(profiledDistributionPeriod));
            
            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ => 
                _.WithCustomProfiles(NewFundingLineOverrides(fl =>
                    fl.WithFundingLineCode(fundingLineWithCustomProfile)
                        .WithDistributionPeriods(customProfile)))
                .WithFundingLines(fundingLineForCustomProfile,
                    fundingLineWithProfiling));

            await WhenThePublishedProviderIsProcessed(publishedProviderVersion);

            fundingLineForCustomProfile
                .DistributionPeriods
                .Should()
                .BeEquivalentTo(customProfile);

            fundingLineWithProfiling
                .DistributionPeriods
                .Should()
                .BeEquivalentTo(profiledDistributionPeriod);
            
            _errorDetection.Verify(_ => _.ProcessPublishedProvider(publishedProviderVersion),
                Times.Once);
        }

        private static RandomString NewRandomString()
        {
            return new RandomString();
        }

        private async Task WhenThePublishedProviderIsProcessed(PublishedProviderVersion publishedProviderVersion)
        {
            await _reApplyCustomProfiles.ProcessPublishedProvider(publishedProviderVersion);
        }

        private PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder providerVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(providerVersionBuilder);
            
            return providerVersionBuilder.Build();
        }

        private FundingLineProfileOverrides NewFundingLineOverrides(Action<FundingLineProfileOverridesBuilder> setUp = null)
        {
            FundingLineProfileOverridesBuilder profileOverridesBuilder = new FundingLineProfileOverridesBuilder();

            setUp?.Invoke(profileOverridesBuilder);
            
            return profileOverridesBuilder.Build();
        }

        private DistributionPeriod NewDistributionPeriod(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder distributionPeriodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(distributionPeriodBuilder);
            
            return distributionPeriodBuilder.Build();
        }

        private ProfilePeriod NewProfilePeriod(Action<ProfilePeriodBuilder> setUp = null)
        {
            ProfilePeriodBuilder profilePeriodBuilder = new ProfilePeriodBuilder();

            setUp?.Invoke(profilePeriodBuilder);
            
            return profilePeriodBuilder.Build();
        }

        private FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);
            
            return fundingLineBuilder.Build();
        }
    }
}