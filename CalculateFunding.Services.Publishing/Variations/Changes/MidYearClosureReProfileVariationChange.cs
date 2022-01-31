using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using ProfilePatternKey = CalculateFunding.Models.Publishing.ProfilePatternKey;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class MidYearClosureReProfileVariationChange : MidYearReProfileVariationChange
    {
        protected override string ChangeName => "Mid year closure re-profile variation change";

        public MidYearClosureReProfileVariationChange(ProviderVariationContext variationContext,
            string strategy) : base(variationContext, strategy)
        {
        }

        protected override Task<(ReProfileRequest, bool)> BuildReProfileRequest(string fundingLineCode,
            PublishedProviderVersion refreshState,
            PublishedProviderVersion priorState,
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