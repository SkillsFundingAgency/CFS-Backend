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
    }
}
