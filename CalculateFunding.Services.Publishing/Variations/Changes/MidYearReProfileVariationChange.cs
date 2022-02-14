using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Profiling;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class MidYearReProfileVariationChange : ReProfileVariationChange
    {
        private readonly string _strategy;

        protected override string ChangeName => "Mid year re-profile variation change";

        protected override bool ShouldPersistReProfileAudit(ReProfileRequest reProfileRequest) => true;

        public MidYearReProfileVariationChange(ProviderVariationContext variationContext,
            string strategy) : base(variationContext, strategy)
        {
            _strategy = strategy;
        }

        protected override IEnumerable<string> GetAffectedFundingLines => VariationContext.AffectedFundingLinesWithVariationPointerSet(_strategy);

        public override bool ReProfileForSameAmountFunc(string fundingLineCode, string profilePatternKey, ReProfileAudit reProfileAudit, int paidUpToIndex)
        {
            bool executeSameAsKey = base.ReProfileForSameAmountFunc(fundingLineCode, profilePatternKey, reProfileAudit, paidUpToIndex);

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
            PublishedProviderVersion currentState,
            IApplyProviderVariations variationApplications,
            string profilePatternKey,
            ReProfileAudit reProfileAudit,
            FundingLine fundingLine,
            Func<string, string, ReProfileAudit, int, bool> reProfileForSameAmountFunc) =>
            variationApplications.ReProfilingRequestBuilder.BuildReProfileRequest(fundingLineCode,
                profilePatternKey,
                BuildCurrentPublishedProvider(currentState, refreshState, fundingLine),
                fundingLine.Value,
                reProfileAudit,
                midYearType: GetMidYearType(refreshState.Provider?.DateOpened, fundingLine),
                reProfileForSameAmountFunc: reProfileForSameAmountFunc);

        protected PublishedProviderVersion BuildCurrentPublishedProvider(PublishedProviderVersion currentState, PublishedProviderVersion refreshState, FundingLine refreshFundingLine)
        {
            if (currentState == null)
            {
                return currentState;
            }

            // copy the current published provider so we don't get side effects
            PublishedProviderVersion currentPublishedProvider = currentState.DeepCopy();

            FundingLine fundingLine = currentPublishedProvider.FundingLines?.SingleOrDefault(_ => _.FundingLineCode == refreshFundingLine.FundingLineCode);

            // the funding line doesn't currently exist this can happen if a new funding line has been added to the template
            if (fundingLine == null)
            {
                // copy the refreshed funding line so we don't get side effects
                fundingLine = refreshFundingLine.DeepCopy();

                // add the funding line to the current published provider
                currentPublishedProvider.FundingLines = (currentPublishedProvider.FundingLines ?? ArraySegment<FundingLine>.Empty).Concat(new[] { fundingLine });
            }
            else if (fundingLine.Value.HasValue && fundingLine.Value != 0)
            {
                // return the prior current state as it has a value it will have distribution periods set
                return currentState;
            }

            // if the current funding line value is null or 0 we need to populate the distribution periods
            foreach (DistributionPeriod distributionPeriod in fundingLine.DistributionPeriods)
            {
                distributionPeriod.Value = 0;

                foreach (ProfilePeriod profilePeriod in distributionPeriod.ProfilePeriods)
                {
                    profilePeriod.ProfiledValue = 0;
                }
            }

            return currentPublishedProvider;
        }

        private MidYearType GetMidYearType(DateTimeOffset? dateTimeOpened, FundingLine fundingLine)
        {
            ProfilePeriod firstPeriod =  new YearMonthOrderedProfilePeriods(fundingLine).ToArray().First();

            DateTimeOffset? openedDate = dateTimeOpened;
            bool catchup = openedDate == null ? false : openedDate.Value.Month < YearMonthOrderedProfilePeriods.MonthNumberFor(firstPeriod.TypeValue) && openedDate.Value.Year <= firstPeriod.Year;
            return catchup ? MidYearType.OpenerCatchup : MidYearType.Opener;
        }
    }
}