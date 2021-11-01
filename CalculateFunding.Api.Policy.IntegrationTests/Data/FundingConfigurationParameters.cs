using CalculateFunding.Common.ApiClient.Policies.Models;
using System.Collections.Generic;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class FundingConfigurationParameters
    {
        public string Id => $"config-{FundingStreamId}-{FundingPeriodId}";
        public string FundingStreamId { get; set; }
        public string FundingPeriodId { get; set; }
        public string DefaultTemplateVersion { get; set; }
        public IEnumerable<string> AllowedPublishedFundingStreamsIdsToReference { get; set; }
        public IEnumerable<FundingVariation> ReleaseManagementVariations { get; set; }
        public IEnumerable<FundingConfigurationChannel> ReleaseChannels { get; set; }
        public IEnumerable<ReleaseActionGroup> ReleaseActionGroups { get; set; }
    }
}
