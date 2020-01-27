using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Variations.Changes;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class ReAdjustFundingValuesForProfileValuesChangeTests : VariationChangeTestBase
    {
        private ReAdjustFundingValuesForProfileValuesChange _change;

        [TestInitialize]
        public void SetUp()
        {
            _change = new ReAdjustFundingValuesForProfileValuesChange(VariationContext);
        }

        [TestMethod]
        public async Task ReCalculatesTheFundingValuesAtEachLevelToAccountForProfileChangesFromVariations()
        {
            GivenTheFundingLines(NewFundingLine(_ => _.WithDistributionPeriods(NewDistributionPeriod(dp => 
                    dp.WithProfilePeriods(NewProfilePeriod(), NewProfilePeriod(), NewProfilePeriod())),
                    NewDistributionPeriod(dp => dp.WithProfilePeriods(NewProfilePeriod(), NewProfilePeriod())))));
            
            await WhenTheChangeIsApplied();
            
            ThenTheDistributionPeriodValuesShouldHaveBeenAdjusted();
            AndTheFundingLineValuesShouldHaveBeenAdjusted();
            AndTheTotalFundingShouldHaveBeenAdjusted();
        }

        private async Task WhenTheChangeIsApplied()
        {
            await _change.Apply(VariationsApplication);
        }
        
        private void ThenTheDistributionPeriodValuesShouldHaveBeenAdjusted()
        {
            foreach (var distributionPeriod in VariationContext.RefreshState.FundingLines.SelectMany(_ => _.DistributionPeriods))
            {
                distributionPeriod
                    .Value
                    .Should()
                    .Be(distributionPeriod.ProfilePeriods.Sum(_ => _.ProfiledValue));
            }
        }

        private void AndTheFundingLineValuesShouldHaveBeenAdjusted()
        {
            foreach (var fundingLine in VariationContext.RefreshState.FundingLines)
            {
                fundingLine
                    .Value
                    .Should()
                    .Be(fundingLine.DistributionPeriods.Sum(_ => _.Value));
            }      
        }

        private void AndTheTotalFundingShouldHaveBeenAdjusted()
        {
            VariationContext.RefreshState
                .TotalFunding
                .Should()
                .Be(VariationContext.RefreshState.FundingLines.Sum(_ => _.Value));
        }
    }
}