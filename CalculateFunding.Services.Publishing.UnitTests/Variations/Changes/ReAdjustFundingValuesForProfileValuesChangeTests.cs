using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Variations.Changes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculateFunding.Services.Publishing.UnitTests.Variations.Changes
{
    [TestClass]
    public class ReAdjustFundingValuesForProfileValuesChangeTests : ReAdjustFundingValuesTestsBase
    {
        [TestInitialize]
        public void SetUp()
        {
            Change = new ReAdjustFundingValuesForProfileValuesChange(VariationContext);

            TargetPublishedProviderVersion = VariationContext.RefreshState;
        }

        [TestMethod]
        public async Task ReCalculatesTheFundingValuesAtEachLevelToAccountForProfileChangesFromVariations()
        {
            GivenTheFundingLines(NewFundingLine(_ => _.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
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