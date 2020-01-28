using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Variations.Changes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class ReAdjustFundingValuesForProfileValuesChangeTests : ReAdjustFundingValuesTestsBase
    {
        private ReAdjustFundingValuesForProfileValuesChange _change;

        [TestInitialize]
        public void SetUp()
        {
            _change = new ReAdjustFundingValuesForProfileValuesChange(VariationContext);

            TargetPublishedProviderVersion = VariationContext.RefreshState;
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
    }
}