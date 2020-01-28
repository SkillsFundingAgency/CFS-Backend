using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Variations.Changes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class ReAdjustSuccessorFundingValuesForProfileValueChangeTests : ReAdjustFundingValuesTestsBase
    {
        private ReAdjustSuccessorFundingValuesForProfileValueChange _change;

        [TestInitialize]
        public void SetUp()
        {
            _change = new ReAdjustSuccessorFundingValuesForProfileValueChange(VariationContext);

            VariationContext.SuccessorRefreshState = VariationContext.RefreshState.DeepCopy();

            TargetPublishedProviderVersion = VariationContext.SuccessorRefreshState;
        }

        [TestMethod]
        public async Task ReCalculatesTheFundingValuesAtEachLevelToAccountForProfileChangesFromVariations()
        {
            GivenTheSuccessorFundingLines(NewFundingLine(_ => _.WithDistributionPeriods(NewDistributionPeriod(dp => 
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