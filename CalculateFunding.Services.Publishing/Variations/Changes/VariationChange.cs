using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Serilog;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public abstract class VariationChange : IVariationChange
    {
        private readonly string _strategyName;

        protected abstract string ChangeName { get; }

        protected VariationChange(ProviderVariationContext variationContext,
            string strategyName)
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
                RecordError(exception);
            }

            variationsApplication.AddPublishedProviderToUpdate(VariationContext.PublishedProvider);

            // if there is a successor then we need to add it to existing providers to update
            if (VariationContext.Successor != null)
            {
                variationsApplication.AddPublishedProviderToUpdate(VariationContext.Successor);
            }
        }

        protected abstract Task ApplyChanges(IApplyProviderVariations variationsApplications);

        protected void RecordError(Exception exception = null, string error = null)
        {
            VariationContext.LogError($"Unable to apply '{ChangeName}' for '{_strategyName}' on provider id '{VariationContext.ProviderId}'{error}", exception);
            VariationContext.RecordError(VariationContext.ProviderId, $"Unable to apply '{ChangeName}' for '{_strategyName}'", error, exception?.Message);
        }
    }
}