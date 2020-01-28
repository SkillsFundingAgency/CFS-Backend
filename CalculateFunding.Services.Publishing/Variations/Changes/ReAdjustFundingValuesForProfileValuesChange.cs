using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class ReAdjustFundingValuesForProfileValuesChange : VariationChange
    {
        public ReAdjustFundingValuesForProfileValuesChange(ProviderVariationContext variationContext) : base(variationContext)
        {
        }

        protected override Task ApplyChanges(IApplyProviderVariations variationsApplications)
        {
            AdjustDistributionPeriodValuesForProfileAmountChanges();
            AdjustFundingLineValuesForDistributionPeriodValueChanges();
            AdjustTotalFundingForProviderForFundingLineValueChanges();
            
            return Task.CompletedTask;
        }

        private void AdjustTotalFundingForProviderForFundingLineValueChanges()
        {
            PublishedProviderToAdjust.TotalFunding = PublishedProviderToAdjust.FundingLines.Sum(_ => _.Value);
        }
        
        private void AdjustFundingLineValuesForDistributionPeriodValueChanges()
        {
            foreach (FundingLine fundingLine in PublishedProviderToAdjust.FundingLines)
            {
                fundingLine.Value = fundingLine.DistributionPeriods.Sum(_ => _.Value);
            }   
        }
        
        private void AdjustDistributionPeriodValuesForProfileAmountChanges()
        {
            foreach (DistributionPeriod distributionPeriod in PublishedProviderToAdjust.FundingLines.SelectMany(_ => _.DistributionPeriods))
            {
                distributionPeriod.Value = distributionPeriod.ProfilePeriods.Sum(_ => _.ProfiledValue);
            }
        }

        protected virtual PublishedProviderVersion PublishedProviderToAdjust => RefreshState;
    }
}