using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class ReProfileVariationChange : VariationChange
    {
        public ReProfileVariationChange(ProviderVariationContext variationContext)
            : base(variationContext)
        {
        }

        protected override async Task ApplyChanges(IApplyProviderVariations variationsApplications)
        {
            Guard.IsNotEmpty(VariationContext.AffectedFundingLineCodes, nameof(VariationContext.AffectedFundingLineCodes));

            PublishedProviderVersion refreshState = RefreshState;

            Task[] reProfileTasks = VariationContext.AffectedFundingLineCodes?.Select(_ =>
                    ReProfileFundingLine(_, refreshState, variationsApplications))
                .ToArray();

            await TaskHelper.WhenAllAndThrow(reProfileTasks);
        }

        private async Task ReProfileFundingLine(string fundingLineCode,
            PublishedProviderVersion refreshState,
            IApplyProviderVariations variationApplications)
        {
            FundingLine fundingLine = refreshState.FundingLines.SingleOrDefault(_ => _.FundingLineCode == fundingLineCode);
            string profilePatternKey =  refreshState.ProfilePatternKeys?.SingleOrDefault(_ => _.FundingLineCode == fundingLineCode)?.Key;

            string providerId = refreshState.ProviderId;
            
            if (fundingLine == null)
            {
                throw new NonRetriableException($"Could not locate funding line {fundingLineCode} for published provider version {providerId}");
            }

            ReProfileRequest reProfileRequest = await BuildReProfileRequest(fundingLineCode, refreshState, variationApplications, providerId, profilePatternKey, fundingLine);

            ReProfileResponse reProfileResponse = (await variationApplications.ResiliencePolicies.ProfilingApiClient.ExecuteAsync(()
                => variationApplications.ProfilingApiClient.ReProfile(reProfileRequest)))?.Content;

            if (reProfileResponse == null)
            {
                throw new NonRetriableException($"Could not re profile funding line {fundingLineCode} for provider {providerId}");
            }

            IEnumerable<DistributionPeriod> distributionPeriods = variationApplications.ReProfilingResponseMapper.MapReProfileResponseIntoDistributionPeriods(reProfileResponse);

            foreach (DistributionPeriod distributionPeriod in distributionPeriods)
            {
                refreshState.UpdateDistributionPeriodForFundingLine(fundingLineCode,
                    distributionPeriod.DistributionPeriodId,
                    distributionPeriod.ProfilePeriods);
            }
        }

        protected virtual Task<ReProfileRequest> BuildReProfileRequest(string fundingLineCode,
            PublishedProviderVersion refreshState,
            IApplyProviderVariations variationApplications,
            string providerId,
            string profilePatternKey,
            FundingLine fundingLine) =>
            variationApplications.ReProfilingRequestBuilder.BuildReProfileRequest(refreshState.FundingStreamId,
                refreshState.SpecificationId,
                refreshState.FundingPeriodId,
                providerId,
                fundingLineCode,
                profilePatternKey,
                ProfileConfigurationType.RuleBased,
                fundingLine.Value);
    }
}