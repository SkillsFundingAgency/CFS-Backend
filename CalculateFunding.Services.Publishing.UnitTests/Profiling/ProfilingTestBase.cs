using System;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Changes;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public abstract class ProfilingTestBase
    {
        protected ProfilePeriod NewProfilePeriod(int occurrence, int year, string month, decimal? amount = null)
        {
            return NewProfilePeriod(_ => _.WithOccurence(occurrence)
                .WithAmount(amount.GetValueOrDefault())
                .WithType(ProfilePeriodType.CalendarMonth)
                .WithYear(year)
                .WithTypeValue(month));
        }

        protected static FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);
            
            return fundingLineBuilder.Build();
        }

        protected static DistributionPeriod NewDistributionPeriod(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder distributionPeriodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(distributionPeriodBuilder);
            
            return distributionPeriodBuilder.Build();
        }

        protected static ProfilePeriod NewProfilePeriod(Action<ProfilePeriodBuilder> setUp = null)
        {
            ProfilePeriodBuilder profilePeriodBuilder = new ProfilePeriodBuilder();
            
            setUp?.Invoke(profilePeriodBuilder);

            return profilePeriodBuilder.Build();
        }

        protected static PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);
            
            return publishedProviderBuilder.Build();
        }

        protected static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder providerVersionBuilder = new PublishedProviderVersionBuilder()
                .WithProvider(NewProvider());

            setUp?.Invoke(providerVersionBuilder);

            return providerVersionBuilder.Build();
        }

        protected static Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }
    }
}