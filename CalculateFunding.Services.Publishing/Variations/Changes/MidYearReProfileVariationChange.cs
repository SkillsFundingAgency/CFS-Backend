using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;
using ProfilePatternKey = CalculateFunding.Models.Publishing.ProfilePatternKey;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class MidYearReProfileVariationChange : ReProfileVariationChange
    {
        private readonly string _strategy;

        protected override bool ShouldPersistReProfileAudit(ReProfileRequest reProfileRequest) => true;

        public MidYearReProfileVariationChange(ProviderVariationContext variationContext,
            string strategy) : base(variationContext, strategy)
        {
            _strategy = strategy;
        }

        protected override IEnumerable<string> GetAffectedFundingLines => VariationContext.AffectedFundingLinesWithVariationPointerSet(_strategy);

        public override bool ReProfileForSameAmountFunc(string fundingLineCode, ReProfileAudit reProfileAudit, int paidUpToIndex)
        {
            bool executeSameAsKey = base.ReProfileForSameAmountFunc(fundingLineCode, reProfileAudit, paidUpToIndex);

            if (!executeSameAsKey)
            {
                int? variationPointerIndex = reProfileAudit?.VariationPointerIndex;

                executeSameAsKey = reProfileAudit == null || variationPointerIndex != paidUpToIndex;
            }

            return executeSameAsKey;
        }

        protected override Task<(ReProfileRequest, bool)> BuildReProfileRequest(string fundingLineCode,
            PublishedProviderVersion refreshState,
            PublishedProviderVersion priorState,
            IApplyProviderVariations variationApplications,
            string profilePatternKey,
            ReProfileAudit reProfileAudit,
            FundingLine fundingLine,
            Func<string, ReProfileAudit, int, bool> reProfileForSameAmountFunc) =>
            variationApplications.ReProfilingRequestBuilder.BuildReProfileRequest(fundingLineCode,
                profilePatternKey,
                refreshState,
                fundingLine.Value,
                reProfileAudit,
                midYearType: GetMidYearType(refreshState.Provider?.DateOpened, fundingLine),
                reProfileForSameAmountFunc: reProfileForSameAmountFunc);

        private MidYearType GetMidYearType(DateTimeOffset? dateTimeOpened, FundingLine fundingLine)
        {
            ProfilePeriod firstPeriod =  new YearMonthOrderedProfilePeriods(fundingLine).ToArray().First();

            DateTimeOffset? openedDate = dateTimeOpened;
            bool catchup = openedDate == null ? false : openedDate.Value.Month < YearMonthOrderedProfilePeriods.MonthNumberFor(firstPeriod.TypeValue) && openedDate.Value.Year <= firstPeriod.Year;
            return catchup ? MidYearType.OpenerCatchup : MidYearType.Opener;
        }
    }
}