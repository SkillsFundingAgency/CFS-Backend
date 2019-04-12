using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class PublishedFundingStreamResult
    {
        [JsonProperty("fundingStream")]
        public PublishedFundingStreamDefinition FundingStream { get; set; }

        [JsonProperty("allocationLineResult")]
        public PublishedAllocationLineResult AllocationLineResult { get; set; }

        [JsonProperty("fundingStreamPeriod")]
        public string FundingStreamPeriod { get; set; }

        [JsonProperty("distributionPeriod")]
        public string DistributionPeriod { get; set; }
    }
}
