using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Changes;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class SetProfilePeriodValuesChangeTests : VariationChangeTestBase
    {
        [TestInitialize]
        public void SetUp()
        {
            Change = new SetProfilePeriodValuesChange(VariationContext);
        }

        [TestMethod]
        public async Task NoChangesToApplyIfPriorStateIsNull()
        {
            await Change.Apply(VariationsApplication);

            VariationContext.RefreshState
                .FundingLines
                .Should()
                .BeNull();
        }

        [TestMethod]
        public async Task ShouldCopyAllDistributionPeriodsFromPriorStateIfAvaiableForFundingLine()
        {
            string fundingLineCodeOne = NewRandomString();
            string fundingLineCodeTwo = NewRandomString();

            GivenThePublishedProviderOriginalSnapshot(VariationContext.ProviderId, NewPublishedProviderSnapShots(
                NewPublishedProvider(_ => _.WithReleased(NewPublishedProviderVersion(
                    v => v.WithFundingLines(
                        NewFundingLine(f => f.WithFundingLineCode(fundingLineCodeOne)
                                            .WithDistributionPeriods(NewDistributionPeriod(), NewDistributionPeriod())),
                        NewFundingLine(f => f.WithFundingLineCode(fundingLineCodeTwo)
                                            .WithDistributionPeriods(NewDistributionPeriod()))))))));

            VariationContext.RefreshState.FundingLines = new[] 
            {
                NewFundingLine(f => f.WithFundingLineCode(fundingLineCodeOne)),
                NewFundingLine(f => f.WithFundingLineCode(fundingLineCodeTwo))
            };

            await Change.Apply(VariationsApplication);

            VariationContext.RefreshState
                .FundingLines.First(f => f.FundingLineCode == fundingLineCodeOne)
                .DistributionPeriods
                .Count()
                .Should()
                .Be(2);

            VariationContext.RefreshState
                .FundingLines.First(f => f.FundingLineCode == fundingLineCodeTwo)
                .DistributionPeriods
                .Count()
                .Should()
                .Be(1);
        }

        private static PublishedProviderSnapShots NewPublishedProviderSnapShots(PublishedProvider publishedProvider)
        {
            return new PublishedProviderSnapShots(publishedProvider);
        }
    }
}
