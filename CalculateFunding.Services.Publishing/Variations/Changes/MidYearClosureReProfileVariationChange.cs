using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class MidYearClosureReProfileVariationChange : MidYearReProfileVariationChange
    {
        protected override string ChangeName => "Mid year closure re-profile variation change";

        public MidYearClosureReProfileVariationChange(ProviderVariationContext variationContext,
            string strategy) : base(variationContext, strategy)
        {
        }

        protected override PublishedProviderVersion GetState(PublishedProviderVersion currentState, PublishedProviderVersion priorState, bool sameAsAmount)
        {
            if (sameAsAmount)
            {
                // if amount hasn't changed then for closure re-profiling we need to copy the released state to the refresh state
                // as we use the last released state to calculate the mid-year closure funding
                return priorState;
            }

            return currentState;
        }

        protected override Task<(ReProfileRequest, bool)> BuildReProfileRequest(string fundingLineCode,
            PublishedProviderVersion refreshState,
            PublishedProviderVersion priorState,
            PublishedProviderVersion currentState,
            IApplyProviderVariations variationApplications,
            string profilePatternKey,
            ReProfileAudit reProfileAudit,
            FundingLine fundingLine,
            Func<string, string, ReProfileAudit, int, bool> reProfileForSameAmountFunc) =>
            variationApplications.ReProfilingRequestBuilder.BuildReProfileRequest(fundingLineCode,
                profilePatternKey,
                priorState,
                fundingLine.Value,
                reProfileAudit,
                midYearType: MidYearType.Closure,
                reProfileForSameAmountFunc: reProfileForSameAmountFunc);
    }
}