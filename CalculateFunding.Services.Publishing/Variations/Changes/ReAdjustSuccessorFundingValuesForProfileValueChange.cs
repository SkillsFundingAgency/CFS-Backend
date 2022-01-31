using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class ReAdjustSuccessorFundingValuesForProfileValueChange : ReAdjustFundingValuesForProfileValuesChange
    {
        protected override string ChangeName => "Re-adjust successor funding values for profile value change";

        public ReAdjustSuccessorFundingValuesForProfileValueChange(ProviderVariationContext variationContext, string strategyName) 
            : base(variationContext, strategyName)
        {
        }

        protected override PublishedProviderVersion PublishedProviderToAdjust => VariationContext.Successor.Current;
    }
}