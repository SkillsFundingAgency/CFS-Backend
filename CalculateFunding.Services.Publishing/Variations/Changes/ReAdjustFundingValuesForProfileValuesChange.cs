using System.Collections.Generic;
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
            PublishedProviderToAdjust.TotalFunding = PaymentFundingLines.Sum(_ => _.Value);
        }
        
        private void AdjustFundingLineValuesForDistributionPeriodValueChanges()
        {
            foreach (FundingLine fundingLine in PaymentFundingLines)
            {
                fundingLine.Value = fundingLine.DistributionPeriods?.Sum(_ => _.Value);
            }   
        }
        
        private void AdjustDistributionPeriodValuesForProfileAmountChanges()
        {
            foreach (DistributionPeriod distributionPeriod in PaymentFundingLines?.SelectMany(_ => _.DistributionPeriods ?? new DistributionPeriod[0]))
            {
                distributionPeriod.Value = distributionPeriod.ProfilePeriods.Sum(_ => _.ProfiledValue);
            }
        }

        protected virtual PublishedProviderVersion PublishedProviderToAdjust => RefreshState;

        protected IEnumerable<FundingLine> PaymentFundingLines =>
            PublishedProviderToAdjust.FundingLines?.Where(_ => _.Type == OrganisationGroupingReason.Payment);
    }
}