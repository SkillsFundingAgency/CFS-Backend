using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class MetaDataVariationsChange : VariationChange
    {
        public MetaDataVariationsChange(ProviderVariationContext variationContext)
            : base(variationContext)
        {
        }

        protected override Task ApplyChanges(IApplyProviderVariations variationsApplications)
        {
            RefreshState.VariationReasons = VariationContext.Result.VariationReasons.ToArray();
            
            return Task.CompletedTask;
        }
    }
}