using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class SetProfilePeriodValuesChange : VariationChange
    {
        public SetProfilePeriodValuesChange(ProviderVariationContext variationContext) : base(variationContext)
        {
        }

        protected override Task ApplyChanges(IApplyProviderVariations variationsApplications)
        {
            PublishedProviderVersion refreshState = RefreshState;
            PublishedProviderVersion priorState = VariationContext.PriorState;

            if(refreshState == null || priorState == null || !refreshState.FundingLines.AnyWithNullCheck() || !priorState.FundingLines.AnyWithNullCheck())
            {
                return Task.CompletedTask;
            }

            foreach (FundingLine fundingLine in refreshState.FundingLines)
            {
                IEnumerable<DistributionPeriod> priorStateDistributionPeriods = priorState.FundingLines
                                                                        .FirstOrDefault(x => x.FundingLineCode == fundingLine.FundingLineCode)?.DistributionPeriods;
                if (priorStateDistributionPeriods != null) 
                {
                    fundingLine.DistributionPeriods = priorStateDistributionPeriods.Select(x => x.Clone()).ToList();
                }
            }

            return Task.CompletedTask;
        }
    }
}
