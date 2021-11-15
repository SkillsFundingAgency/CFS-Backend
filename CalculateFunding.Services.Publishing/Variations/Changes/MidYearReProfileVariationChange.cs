using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class MidYearReProfileVariationChange : ReProfileVariationChange
    {
        private readonly string _strategy;

        public MidYearReProfileVariationChange(ProviderVariationContext variationContext,
            string strategy) : base(variationContext, strategy)
        {
            _strategy = strategy;
        }

        protected override IEnumerable<string> GetAffectedFundingLines => VariationContext.AffectedFundingLinesWithVariationPointerSet(_strategy);
        
        protected override Task<ReProfileRequest> BuildReProfileRequest(string fundingLineCode,
            PublishedProviderVersion refreshState,
            PublishedProviderVersion priorState,
            IApplyProviderVariations variationApplications,
            string profilePatternKey,
            FundingLine fundingLine) =>
            variationApplications.ReProfilingRequestBuilder.BuildReProfileRequest(fundingLineCode,
                profilePatternKey,
                refreshState,
                ProfileConfigurationType.RuleBased,
                fundingLine.Value,
                midYear: true);
    }
}