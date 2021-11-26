using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public abstract class VariationChange : IVariationChange
    {
        private readonly string _strategyName;
        protected VariationChange(ProviderVariationContext variationContext, string strategyName)
        {
            Guard.ArgumentNotNull(variationContext, nameof(variationContext));
            Guard.IsNullOrWhiteSpace(strategyName, nameof(strategyName));

            VariationContext = variationContext;
            _strategyName = strategyName;
        }

        public ProviderVariationContext VariationContext { get; }

        protected PublishedProviderVersion RefreshState => VariationContext.RefreshState;

        protected PublishedProviderVersion SuccessorRefreshState => VariationContext.Successor.Current;

        protected string ProviderId => VariationContext.ProviderId;

        protected void AddAffectedFundingLine(string fundingLineCode)
        {
            VariationContext.AddAffectedFundingLineCode(_strategyName, fundingLineCode);
        }
        
        public async Task Apply(IApplyProviderVariations variationsApplication)
        {
            try
            {
                await ApplyChanges(variationsApplication);
            }
            catch (Exception exception)
            {
                RecordErrors($"Unable to {_strategyName} for provider id {VariationContext.ProviderId}. {exception.Message}");
            }

            variationsApplication.AddPublishedProviderToUpdate(VariationContext.PublishedProvider);

            // if there is a successor then we need to add it to existing providers to update
            if (VariationContext.Successor != null)
            {
                variationsApplication.AddPublishedProviderToUpdate(VariationContext.Successor);
            }
        }

        protected abstract Task ApplyChanges(IApplyProviderVariations variationsApplications);

        protected void RecordErrors(params string[] error)
        {
            VariationContext.RecordErrors(error);
        }
    }
}