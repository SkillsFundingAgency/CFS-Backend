using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public interface IPublishedProviderFundingSummaryProcessor
    {
        Task<ReleaseFundingPublishedProvidersSummary> GetFundingSummaryForApprovedPublishedProvidersByChannel(IEnumerable<string> publishedProviderIds,
            SpecificationSummary specificationSummary,
            Common.ApiClient.Policies.Models.FundingConfig.FundingConfiguration fundingConfiguration,
            IEnumerable<string> channelCodes);
    }
}