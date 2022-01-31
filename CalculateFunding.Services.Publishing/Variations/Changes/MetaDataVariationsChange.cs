using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class MetaDataVariationsChange : VariationChange
    {
        protected override string ChangeName => "Metadata variations change";

        public MetaDataVariationsChange(ProviderVariationContext variationContext, string strategyName)
            : base(variationContext, strategyName)
        {
        }

        protected override Task ApplyChanges(IApplyProviderVariations variationsApplications)
        {
            RefreshState.VariationReasons = VariationContext.VariationReasons.ToArray();
            
            return Task.CompletedTask;
        }
    }
}