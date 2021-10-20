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
    public class MidYearClosureReProfilingVariationStrategy : VariationStrategy, IVariationStrategy
    {
        public override string Name => "MidYearClosureReProfiling";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext,
            IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProviderVersion priorState = providerVariationContext.PriorState;

            if (VariationPointersNotSet(providerVariationContext) ||
                priorState.Provider.Status == Closed ||
                providerVariationContext.UpdatedProvider.Status != Closed ||
                HasNoReleasedAllocations(providerVariationContext, priorState))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        private bool HasNoReleasedAllocations(ProviderVariationContext providerVariationContext,
            PublishedProviderVersion priorState)
        {
            if (priorState == null)
            {
                return true;
            }

            bool hasNoReleasedAllocations = true;

            foreach (FundingLine latestFundingLine in priorState.PaymentFundingLinesWithValues)
            {
                providerVariationContext.AddAffectedFundingLineCode(latestFundingLine.FundingLineCode);

                hasNoReleasedAllocations = false;
            }

            return hasNoReleasedAllocations;
        }

        private static bool VariationPointersNotSet(ProviderVariationContext providerVariationContext) => !(providerVariationContext.VariationPointers?.Any()).GetValueOrDefault();

        protected override Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            providerVariationContext.QueueVariationChange(new MidYearReProfileVariationChange(providerVariationContext));

            // Stop subsequent strategies                    
            return Task.FromResult(true);
        }
    }
}