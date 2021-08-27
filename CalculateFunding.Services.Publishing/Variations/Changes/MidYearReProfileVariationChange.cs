using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class MidYearReProfileVariationChange : ReProfileVariationChange
    {
        public MidYearReProfileVariationChange(ProviderVariationContext variationContext) 
            : base(variationContext)
        {
        }

        protected override Task<ReProfileRequest> BuildReProfileRequest(string fundingLineCode,
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
                fundingLine.Value,
                true,
                refreshState.Provider?.DateOpened);
    }
}