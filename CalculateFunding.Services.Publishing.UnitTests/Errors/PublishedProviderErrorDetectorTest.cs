using System;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;

namespace CalculateFunding.Services.Publishing.UnitTests.Errors
{
    public abstract class PublishedProviderErrorDetectorTest
    {
        protected void AndPublishedProviderShouldHaveTheErrors(PublishedProviderVersion providerVersion,
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

        protected static PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        protected static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        protected static FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);

            return fundingLineBuilder.Build();
        }

        protected static DistributionPeriod NewDistributionPeriod(Action<DistributionPeriodBuilder> setUp = null)
        {
            DistributionPeriodBuilder periodBuilder = new DistributionPeriodBuilder();

            setUp?.Invoke(periodBuilder);

            return periodBuilder.Build();
        }

        protected static ProfilePeriod NewProfilePeriod(Action<ProfilePeriodBuilder> setUp = null)
        {
            ProfilePeriodBuilder periodBuilder = new ProfilePeriodBuilder();

            setUp?.Invoke(periodBuilder);

            return periodBuilder.Build();
        }

        protected static PublishedProviderError NewError(Action<PublishedProviderErrorBuilder> setUp = null)
        {
            PublishedProviderErrorBuilder providerErrorBuilder = new PublishedProviderErrorBuilder();

            setUp?.Invoke(providerErrorBuilder);

            return providerErrorBuilder.Build();
        }

        protected ProfilePatternKey NewProfilePatternKey(Action<ProfilePatternKeyBuilder> setUp = null)
        {
            ProfilePatternKeyBuilder patternKeyBuilder = new ProfilePatternKeyBuilder();

            setUp?.Invoke(patternKeyBuilder);

            return patternKeyBuilder.Build();
        }

        protected ProfilingCarryOver NewProfilingCarryOver(Action<ProfilingCarryOverBuilder> setUp = null)
        {
            ProfilingCarryOverBuilder profilingCarryOverBuilder = new ProfilingCarryOverBuilder();

            setUp?.Invoke(profilingCarryOverBuilder);
            
            return profilingCarryOverBuilder.Build();
        }

        protected string NewRandomString() => new RandomString();
    }
}