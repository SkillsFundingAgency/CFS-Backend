using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Polly;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public abstract class VariationChange : IVariationChange
    {
        protected VariationChange(ProviderVariationContext variationContext)
        {
            Guard.ArgumentNotNull(variationContext, nameof(variationContext));
            
            VariationContext = variationContext;
        }

        public ProviderVariationContext VariationContext { get; }

        protected PublishedProviderVersion RefreshState => VariationContext.RefreshState;

        protected string ProviderId => VariationContext.ProviderId;
        
        public async Task Apply(IApplyProviderVariations variationsApplication)
        {
            await ApplyChanges(variationsApplication);
            
            variationsApplication.AddPublishedProviderToUpdate(VariationContext.PublishedProvider);
        }

        protected abstract Task ApplyChanges(IApplyProviderVariations variationsApplications);

        protected void RecordErrors(params string[] error)
        {
            VariationContext.RecordErrors(error);
        }
    }
}