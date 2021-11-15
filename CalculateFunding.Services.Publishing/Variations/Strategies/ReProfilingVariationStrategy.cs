using System;
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
    public class ReProfilingVariationStrategy : ProfilingChangeVariation, IVariationStrategy
    {
        public override string Name => "ReProfiling";

        protected override Task<bool> Determine(ProviderVariationContext providerVariationContext,
            IEnumerable<string> fundingLineCodes)
        {
            Guard.ArgumentNotNull(providerVariationContext, nameof(providerVariationContext));

            PublishedProviderVersion priorState = providerVariationContext.PriorState;
            PublishedProviderVersion refreshState = providerVariationContext.RefreshState;

            if (priorState == null ||
                providerVariationContext.ReleasedState == null ||
                priorState.Provider.Status == Closed ||
                providerVariationContext.UpdatedProvider.Status == Closed ||
                HasNoProfilingChanges(priorState, refreshState, providerVariationContext) ||
                HasNoPaidPeriods(providerVariationContext, priorState))
            {
                return Task.FromResult(false);
            }

            return Task.FromResult(true);
        }

        protected override bool ExtraFundingLinePredicate(PublishedProviderVersion refreshState,
            FundingLine fundingLine)
            => !refreshState.FundingLineHasCustomProfile(fundingLine.FundingLineCode);

        protected override bool HasNoProfilingChanges(PublishedProviderVersion priorState,
            PublishedProviderVersion refreshState,
            ProviderVariationContext providerVariationContext) =>
            base.HasNoProfilingChanges(priorState, refreshState, providerVariationContext) &&
            HasNoCarryOverChanges(priorState, refreshState, providerVariationContext);

        protected override Task<bool> Execute(ProviderVariationContext providerVariationContext)
        {
            providerVariationContext.QueueVariationChange(new ReProfileVariationChange(providerVariationContext, Name));

            return Task.FromResult(false);
        }

        private bool HasNoCarryOverChanges(PublishedProviderVersion priorState,
            PublishedProviderVersion refreshState,
            ProviderVariationContext providerVariationContext)
        {
            bool hasNoCarryOverChanges = true;

            foreach (ProfilingCarryOver carryOver in priorState.CarryOvers ?? ArraySegment<ProfilingCarryOver>.Empty)
            {
                ProfilingCarryOver latestCustomProfile = refreshState.CarryOvers?.SingleOrDefault(_ => _.FundingLineCode == carryOver.FundingLineCode);

                if ((latestCustomProfile?.Amount).GetValueOrDefault() != carryOver.Amount)
                {
                    providerVariationContext.AddAffectedFundingLineCode(Name, carryOver.FundingLineCode);

                    hasNoCarryOverChanges = false;
                }
            }

            return hasNoCarryOverChanges;
        }
    }
}