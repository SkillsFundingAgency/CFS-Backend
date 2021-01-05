using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.Variations.Changes;

namespace CalculateFunding.Services.Publishing.Variations.Strategies
{
    public class ReProfilingVariationStrategy : ProfilingChangeVariation, IVariationStrategy
    {
        public string Name => "ReProfiling";

        public Task DetermineVariations(ProviderVariationContext providerVariationContext,
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
                return Task.CompletedTask;
            }

            providerVariationContext.QueueVariationChange(new ReProfileVariationChange(providerVariationContext));

            return Task.CompletedTask;
        }

        private bool HasNoPaidPeriods(ProviderVariationContext providerVariationContext,
            PublishedProviderVersion priorState)
        {
            foreach (ProfileVariationPointer variationPointer in providerVariationContext.VariationPointers ?? ArraySegment<ProfileVariationPointer>.Empty)
            {
                FundingLine fundingLine = priorState?.FundingLines.SingleOrDefault(_ => _.FundingLineCode == variationPointer.FundingLineId);

                if (fundingLine == null)
                {
                    continue;
                }

                YearMonthOrderedProfilePeriods periods = new YearMonthOrderedProfilePeriods(fundingLine);

                int variationPointerIndex = periods.IndexOf(_ => _.Occurrence == variationPointer.Occurrence &&
                                                                 _.Type.ToString() == variationPointer.PeriodType &&
                                                                 _.Year == variationPointer.Year &&
                                                                 _.TypeValue == variationPointer.TypeValue);

                if (variationPointerIndex > 0)
                {
                    return false;
                }
            }

            return true;
        }

        protected override bool ExtraFundingLinePredicate(PublishedProviderVersion refreshState,
            FundingLine fundingLine)
            => !refreshState.FundingLineHasCustomProfile(fundingLine.FundingLineCode);

        protected override bool HasNoProfilingChanges(PublishedProviderVersion priorState,
            PublishedProviderVersion refreshState,
            ProviderVariationContext providerVariationContext) =>
            base.HasNoProfilingChanges(priorState, refreshState, providerVariationContext) &&
            HasNoCarryOverChanges(priorState, refreshState, providerVariationContext);

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
                    providerVariationContext.AddAffectedFundingLineCode(carryOver.FundingLineCode);

                    hasNoCarryOverChanges = false;
                }
            }

            return hasNoCarryOverChanges;
        }
    }
}