using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Variations.Changes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class ReAdjustSuccessorFundingValuesForProfileValueChangeTests : ReAdjustFundingValuesTestsBase
    {
        [TestInitialize]
        public void SetUp()
        {
            Change = new ReAdjustSuccessorFundingValuesForProfileValueChange(VariationContext);

            VariationContext.Successor = new PublishedProvider { Current = VariationContext.RefreshState.DeepCopy() };

            TargetPublishedProviderVersion = VariationContext.Successor.Current;
        }

        [TestMethod]
        public async Task ReCalculatesTheFundingValuesAtEachLevelToAccountForProfileChangesFromVariations()
        {
            GivenTheSuccessorFundingLines(NewFundingLine(_ => _.WithFundingLineType(FundingLineType.Payment)
                    .WithDistributionPeriods(NewDistributionPeriod(dp => dp.WithProfilePeriods(NewProfilePeriod(), 
                        NewProfilePeriod(), 
                        NewProfilePeriod())),
                NewDistributionPeriod(dp => dp.WithProfilePeriods(NewProfilePeriod(),
                    NewProfilePeriod())))));
            
            await WhenTheChangeIsApplied();
            
            ThenTheDistributionPeriodValuesShouldHaveBeenAdjusted();
            AndTheFundingLineValuesShouldHaveBeenAdjusted();
            AndTheTotalFundingShouldHaveBeenAdjusted();
        }
    }
}