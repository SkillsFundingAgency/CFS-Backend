using System;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Profiling.Custom;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling.Overrides
{
    public abstract class CustomProfileRequestTestBase
    {
        protected FundingLineProfileOverrides[] NewProfileOverrides(params FundingLineProfileOverrides[] overrides) => overrides;

        protected ApplyCustomProfileRequest NewApplyCustomProfileRequest(Action<ApplyCustomProfileRequestBuilder> setUp = null)
        {
            ApplyCustomProfileRequestBuilder requestBuilder = new ApplyCustomProfileRequestBuilder();

            setUp?.Invoke(requestBuilder);

            return requestBuilder.Build();
        }

        protected FundingLineProfileOverrides NewFundingLineProfileOverrides(Action<FundingLineProfileOverridesBuilder> setUp = null)
        {
            FundingLineProfileOverridesBuilder profileOverridesBuilder = new FundingLineProfileOverridesBuilder();

            setUp?.Invoke(profileOverridesBuilder);

            return profileOverridesBuilder.Build();
        }

        protected FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        protected DistributionPeriod NewDistributionPeriod(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder distributionPeriodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(distributionPeriodBuilder);

            return distributionPeriodBuilder.Build();
        }

        protected ProfilePeriod NewProfilePeriod(Action<ProfilePeriodBuilder> setUp = null)
        {
            ProfilePeriodBuilder profilePeriodBuilder = new ProfilePeriodBuilder();

            setUp?.Invoke(profilePeriodBuilder);

            return profilePeriodBuilder.Build();
        }

        protected PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder providerBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        protected PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder providerVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(providerVersionBuilder);

            return providerVersionBuilder.Build();
        }

        protected static string NewRandomString()
        {
            return new RandomString();
        }
    }
}