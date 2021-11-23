using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class ProfilingUpdatedVariationStrategy : ProfilingChangeVariation, IVariationStrategy
    {
        public override string Name => "ProfilingUpdated";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext, IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProviderVersion priorState = providerVariationContext.PriorState;

            if (priorState == null ||
                providerVariationContext.ReleasedState == null ||
                priorState.Provider.Status == Closed ||
                providerVariationContext.UpdatedProvider.Status == Closed )
            {
                return Task.FromResult(false);
            }

            IEnumerable<string> fundingLinesWithProfilingChanges = FundingLinesWithProfilingChanges(priorState, providerVariationContext.RefreshState);

            if (fundingLinesWithProfilingChanges.IsNullOrEmpty())
            {
                return Task.FromResult(false);
            }

            fundingLinesWithProfilingChanges.ForEach(_ => providerVariationContext.AddAffectedFundingLineCode(Name, _));

            return Task.FromResult(true);
        }

        protected override Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            providerVariationContext.AddVariationReasons(VariationReason.ProfilingUpdated);

            providerVariationContext.QueueVariationChange(new MetaDataVariationsChange(providerVariationContext, Name));

            return Task.FromResult(false);
        }
    }
}