using System.Linq;
using CalculateFunding.Models.Publishing;
using FluentAssertions;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    public abstract class ReAdjustFundingValuesTestsBase : VariationChangeTestBase
    {
        protected PublishedProviderVersion TargetPublishedProviderVersion;
        
        protected void ThenTheDistributionPeriodValuesShouldHaveBeenAdjusted()
        {
            foreach (DistributionPeriod distributionPeriod in TargetPublishedProviderVersion.FundingLines.SelectMany(_ => _.DistributionPeriods))
            {
                distributionPeriod
                    .Value
                    .Should()
                    .Be(distributionPeriod.ProfilePeriods.Sum(_ => _.ProfiledValue));
            }
        }

        protected void AndTheFundingLineValuesShouldHaveBeenAdjusted()
        {
            foreach (FundingLine fundingLine in TargetPublishedProviderVersion.FundingLines)
            {
                fundingLine
                    .Value
                    .Should()
                    .Be(fundingLine.DistributionPeriods.Sum(_ => _.Value));
            }      
        }

        protected void AndTheTotalFundingShouldHaveBeenAdjusted()
        {
            TargetPublishedProviderVersion
                .TotalFunding
                .Should()
                .Be(TargetPublishedProviderVersion.FundingLines.Sum(_ => _.Value));
        }
    }
}