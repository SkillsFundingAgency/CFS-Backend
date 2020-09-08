using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.UnitTests.Errors;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Changes;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public abstract class ProfilingTestBase
    {
        protected static ProfilePeriod NewProfilePeriod(int occurrence, int year, string month, decimal? amount = null)
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

        protected static IEnumerable<FundingLine> NewFundingLines(params Action<FundingLineBuilder>[] setUp)
        {
            return setUp.Select(NewFundingLine);
        }

        protected static FundingCalculation NewFundingCalculation(Action<FundingCalculationBuilder> setUp = null)
        {
            FundingCalculationBuilder fundingCalculationBuilder = new FundingCalculationBuilder();

            setUp?.Invoke(fundingCalculationBuilder);

            return fundingCalculationBuilder.Build();
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

        protected static ProfilePatternKey NewProfilePatternKey(Action<ProfilePatternKeyBuilder> setUp = null)
        {
            ProfilePatternKeyBuilder profilePatternKeyBuilder = new ProfilePatternKeyBuilder();

            setUp?.Invoke(profilePatternKeyBuilder);

            return profilePatternKeyBuilder.Build();
        }

        protected static IEnumerable<ProfilePatternKey> NewProfilePatternKeys(params Action<ProfilePatternKeyBuilder>[] setUp)
        {
            return setUp.Select(NewProfilePatternKey);
        }

        protected static ProfilingCarryOver NewProfilingCarryOver(Action<ProfilingCarryOverBuilder> setUp = null)
        {
            ProfilingCarryOverBuilder profilingCarryOverBuilder = new ProfilingCarryOverBuilder();

            setUp?.Invoke(profilingCarryOverBuilder);

            return profilingCarryOverBuilder.Build();
        }

        protected static IEnumerable<ProfilingCarryOver> NewProfilingCarryOvers(params Action<ProfilingCarryOverBuilder>[] setUp)
        {
            return setUp.Select(NewProfilingCarryOver);
        }

        protected static ProfilingAudit NewProfilingAudit(Action<ProfilingAuditBuilder> setUp = null)
        {
            ProfilingAuditBuilder profilingAuditBuilder = new ProfilingAuditBuilder();

            setUp?.Invoke(profilingAuditBuilder);

            return profilingAuditBuilder.Build();
        }

        protected static IEnumerable<ProfilingAudit> NewProfilingAudits(params Action<ProfilingAuditBuilder>[] setUp)
        {
            return setUp.Select(NewProfilingAudit);
        }

        protected static Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        protected static ProfileVariationPointer NewProfileVariationPointer(Action<ProfileVariationPointerBuilder> setUp = null)
        {
            ProfileVariationPointerBuilder profileVariationPointerBuilder = new ProfileVariationPointerBuilder();

            setUp?.Invoke(profileVariationPointerBuilder);

            return profileVariationPointerBuilder.Build();
        }

        protected static IEnumerable<ProfileVariationPointer> NewProfileVariationPointers(
            params Action<ProfileVariationPointerBuilder>[] setUp)
        {
            return setUp.Select(NewProfileVariationPointer);
        }

        protected static FundingLineProfile NewFundingLineProfile(Action<FundingLineProfileBuilder> setUp = null)
        {
            FundingLineProfileBuilder fundingLineProfileBuilder = new FundingLineProfileBuilder();

            setUp?.Invoke(fundingLineProfileBuilder);

            return fundingLineProfileBuilder.Build();
        }

        protected static FundingDate NewFundingDate(Action<FundingDateBuilder> setUp = null)
        {
            FundingDateBuilder fundingDateBuilder = new FundingDateBuilder();

            setUp?.Invoke(fundingDateBuilder);

            return fundingDateBuilder.Build();
        }

        protected static FundingStream NewFundingStream(Action<FundingStreamBuilder> setUp = null)
        {
            FundingStreamBuilder fundingStreamBuilder = new FundingStreamBuilder();

            setUp?.Invoke(fundingStreamBuilder);

            return fundingStreamBuilder.Build();
        }

        protected static FundingDatePattern NewFundingDatePattern(Action<FundingDatePatternBuilder> setUp = null)
        {
            FundingDatePatternBuilder fundingDatePatternBuilder = new FundingDatePatternBuilder();

            setUp?.Invoke(fundingDatePatternBuilder);

            return fundingDatePatternBuilder.Build();
        }

        protected static FundingLineChange NewFundingLineChange(Action<FundingLineChangeBuilder> setUp = null)
        {
            FundingLineChangeBuilder fundingLineChangeBuilder = new FundingLineChangeBuilder();

            setUp?.Invoke(fundingLineChangeBuilder);

            return fundingLineChangeBuilder.Build();
        }

        protected static IEnumerable<FundingDatePattern> NewFundingDatePatterns(
    params Action<FundingDatePatternBuilder>[] setUp)
        {
            return setUp.Select(NewFundingDatePattern);
        }
    }
}