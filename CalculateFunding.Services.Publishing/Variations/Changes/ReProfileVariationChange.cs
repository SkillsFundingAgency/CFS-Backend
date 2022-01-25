using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class ReProfileVariationChange : VariationChange
    {
        private readonly string _strategy;

        public ReProfileVariationChange(ProviderVariationContext variationContext,
            string strategy) : base(variationContext, strategy)
        {
            _strategy = strategy;
        }

        protected virtual IEnumerable<string> GetAffectedFundingLines => VariationContext.AffectedFundingLineCodes(_strategy);

        // make sure we only persist the re-profile audit if the fundingline value has changed
        protected virtual bool ShouldPersistReProfileAudit(ReProfileRequest reProfileRequest) => reProfileRequest?.FundingLineTotal == reProfileRequest?.ExistingFundingLineTotal ? false : true;

        protected override async Task ApplyChanges(IApplyProviderVariations variationsApplications)
        {
            Guard.IsNotEmpty(VariationContext.AffectedFundingLineCodes(_strategy), nameof(VariationContext.AffectedFundingLineCodes));

            PublishedProviderVersion refreshState = RefreshState;
            PublishedProviderVersion priorState = VariationContext.PriorState;
            PublishedProviderVersion currentState = VariationContext.CurrentState;

            Task[] reProfileTasks = GetAffectedFundingLines.Select(_ =>
                    ReProfileFundingLine(_, refreshState, priorState, variationsApplications, currentState))
                .ToArray();

            await TaskHelper.WhenAllAndThrow(reProfileTasks);
        }

        public virtual bool ReProfileForSameAmountFunc(string fundingLineCode, string profilePatternKey, ReProfileAudit reProfileAudit, int paidToIndex)
        {
            string profileETag = reProfileAudit?.ETag;

            return !string.IsNullOrWhiteSpace(profileETag) && profileETag != GetProfilePattern(fundingLineCode, profilePatternKey)?.ETag;
        }

        public bool SkipReProfiling(ReProfileRequest reProfileRequest, bool reProfileForSameAmount)
        {
            if (reProfileRequest.VariationPointerIndex > reProfileRequest.ExistingPeriods.Count())
            {
                // this will only be the case if we're forcing a refresh on the same variation index
                // as we increment the variation pointer so that we skip the already paid profile period
                // but if the variation pointer that we are forcing on is the last profile period then we need
                // to skip re-profiling
                return true;
            }

            return !reProfileForSameAmount && reProfileRequest?.FundingLineTotal == reProfileRequest?.ExistingFundingLineTotal;
        }

        private async Task ReProfileFundingLine(string fundingLineCode,
            PublishedProviderVersion refreshState,
            PublishedProviderVersion priorState,
            IApplyProviderVariations variationApplications,
            PublishedProviderVersion currentState)
        {
            FundingLine fundingLine = refreshState.FundingLines.SingleOrDefault(_ => _.FundingLineCode == fundingLineCode);

            string profilePatternKey = refreshState.ProfilePatternKeys?.SingleOrDefault(_ => _.FundingLineCode == fundingLineCode)?.Key;

            ReProfileAudit reProfileAudit = refreshState.ReProfileAudits?.SingleOrDefault(_ => _.FundingLineCode == fundingLineCode);

            string providerId = refreshState.ProviderId;
            
            if (fundingLine == null)
            {
                throw new NonRetriableException($"Could not locate funding line {fundingLineCode} for published provider version {providerId}");
            }

            if (!fundingLine.Value.HasValue)
            {
                // exit early as nothing to re-profile
                return;
            }

            (ReProfileRequest ReProfileRequest, bool ReProfileForSameAmount) = await BuildReProfileRequest(fundingLineCode, refreshState, priorState , variationApplications, profilePatternKey, reProfileAudit, fundingLine,(fundingLineCode, profilePatternKey, reProfileAudit, paidIndex) => ReProfileForSameAmountFunc(fundingLineCode, profilePatternKey, reProfileAudit, paidIndex));

            bool skipReProfiling = SkipReProfiling(ReProfileRequest, ReProfileForSameAmount);
                
            ReProfileResponse reProfileResponse = null;

            if (!skipReProfiling)
            {
                reProfileResponse = (await variationApplications.ResiliencePolicies.ProfilingApiClient.ExecuteAsync(()
                => variationApplications.ProfilingApiClient.ReProfile(ReProfileRequest)))?.Content;

                if (reProfileResponse == null)
                {
                    throw new NonRetriableException($"Could not re profile funding line {fundingLineCode} for provider {providerId} with request: {ReProfileRequest?.AsJson()}");
                }

                skipReProfiling = reProfileResponse.SkipReProfiling;
            }

            // always reset the etag so we don't keep forcing re-profiling
            // the only time the re-profile tag won't exists is if this is the
            // first time re-profiling has been called for this funding line
            // in this case nothing needs to be persisted for the etag 
            refreshState.UpdateReProfileAuditETag(new ReProfileAudit
            {
                FundingLineCode = fundingLineCode,
                ETag = GetProfilePattern(fundingLineCode, profilePatternKey)?.ETag
            });

            if (skipReProfiling)
            {
                FundingLine currentFundingLine = currentState.FundingLines?.SingleOrDefault(_ => _.FundingLineCode == fundingLineCode);

                if (currentFundingLine == null)
                {
                    throw new NonRetriableException($"Could not re profile funding line {fundingLineCode} for provider {providerId} as no current funding line exists");
                }

                foreach (DistributionPeriod distributionPeriod in currentFundingLine.DistributionPeriods)
                {
                    refreshState.UpdateDistributionPeriodForFundingLine(fundingLineCode,
                        distributionPeriod.DistributionPeriodId,
                        distributionPeriod.ProfilePeriods,
                        distributionPeriod);
                }

                return;
            } 

            if (ShouldPersistReProfileAudit(ReProfileRequest))
            {
                refreshState.AddOrUpdateReProfileAudit(new ReProfileAudit
                {
                    FundingLineCode = fundingLineCode,
                    ETag = GetProfilePattern(fundingLineCode, profilePatternKey)?.ETag,
                    VariationPointerIndex = ReProfileRequest.VariationPointerIndex,
                    StrategyKey = reProfileResponse.StrategyKey
                });
            }

            IEnumerable<DistributionPeriod> distributionPeriods = variationApplications.ReProfilingResponseMapper.MapReProfileResponseIntoDistributionPeriods(reProfileResponse);

            foreach (DistributionPeriod distributionPeriod in distributionPeriods)
            {
                refreshState.UpdateDistributionPeriodForFundingLine(fundingLineCode,
                    distributionPeriod.DistributionPeriodId,
                    distributionPeriod.ProfilePeriods,
                    distributionPeriod);
            }
        }

        private FundingStreamPeriodProfilePattern GetProfilePattern(string fundingLineCode, string profilePatternKey)
        {
            string combinedProfilePatternKey = string.IsNullOrWhiteSpace(profilePatternKey) ? fundingLineCode : $"{fundingLineCode}-{profilePatternKey}";

            if (VariationContext.ProfilePatterns.AnyWithNullCheck() && VariationContext.ProfilePatterns.ContainsKey(combinedProfilePatternKey))
            {
                return VariationContext.ProfilePatterns[combinedProfilePatternKey];
            }

            return null;
        }

        protected virtual async Task<(ReProfileRequest request, bool shouldExecuteForSameAsKey)> BuildReProfileRequest(string fundingLineCode,
            PublishedProviderVersion refreshState,
            PublishedProviderVersion priorState,
            IApplyProviderVariations variationApplications,
            string profilePatternKey,
            ReProfileAudit reProfileAudit,
            FundingLine fundingLine,
            Func<string, string, ReProfileAudit, int, bool> reProfileForSameAmountFunc)
        {
            (ReProfileRequest reProfileRequest, bool shouldExecuteForSameAsKey) = await variationApplications.ReProfilingRequestBuilder.BuildReProfileRequest(fundingLineCode,
                profilePatternKey,
                priorState,
                fundingLine.Value,
                reProfileAudit,
                reProfileForSameAmountFunc: reProfileForSameAmountFunc);

            return (reProfileRequest, shouldExecuteForSameAsKey);
        }
    }
}