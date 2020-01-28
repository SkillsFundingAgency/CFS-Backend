using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class ReAdjustSuccessorFundingValuesForProfileValueChange : ReAdjustFundingValuesForProfileValuesChange
    {
        public ReAdjustSuccessorFundingValuesForProfileValueChange(ProviderVariationContext variationContext) 
            : base(variationContext)
        {
        }

        protected override PublishedProviderVersion PublishedProviderToAdjust => VariationContext.SuccessorRefreshState;
    }
}