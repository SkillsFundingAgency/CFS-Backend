using System;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Profiling.Custom;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Changes;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling.Overrides
{
    public abstract class CustomProfileRequestTestBase
    {
        protected ProfilePeriod[] NewProfilePeriods(params ProfilePeriod[] profilePeriods) => profilePeriods;

        protected ApplyCustomProfileRequest NewApplyCustomProfileRequest(Action<ApplyCustomProfileRequestBuilder> setUp = null)
        {
            ApplyCustomProfileRequestBuilder requestBuilder = new ApplyCustomProfileRequestBuilder();

            setUp?.Invoke(requestBuilder);

            return requestBuilder.Build();
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

        protected FundingConfiguration NewFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder fundingConfigurationBuilder = new FundingConfigurationBuilder();

            setUp?.Invoke(fundingConfigurationBuilder);

            return fundingConfigurationBuilder.Build();
        }

        protected SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setup = null)
            => BuildNewModel<SpecificationSummary, SpecificationSummaryBuilder>(setup);

        protected ProfileVariationPointer NewProfileVariationPointer(Action<ProfileVariationPointerBuilder> setup = null)
            => BuildNewModel<ProfileVariationPointer, ProfileVariationPointerBuilder>(setup);

        protected Provider NewProvider(Action<ProviderBuilder> setup = null)
            => BuildNewModel<Provider, ProviderBuilder>(setup);

        protected OrganisationGroupResult NewOrganisationGroupResult(Action<OrganisationGroupResultBuilder> setup = null)
            => BuildNewModel<OrganisationGroupResult, OrganisationGroupResultBuilder>(setup);

        private T BuildNewModel<T, TB>(Action<TB> setup) where TB : TestEntityBuilder, new()
        {
            dynamic builder = new TB();
            setup?.Invoke(builder);
            return builder.Build();
        }

        protected static string NewRandomString()
            => new RandomString();

        protected static int NewRandomNumber()
            => new RandomNumberBetween(1, 1000);
    }
}