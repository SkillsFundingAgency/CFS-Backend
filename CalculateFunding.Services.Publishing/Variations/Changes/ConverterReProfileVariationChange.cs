using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Variations.Changes
{
    public class ConverterReProfileVariationChange : MidYearReProfileVariationChange
    {
        public ConverterReProfileVariationChange(ProviderVariationContext variationContext,
            string strategy) : base(variationContext, strategy)
        {
        }

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
                midYearType: MidYearType.Converter);
    }
}
