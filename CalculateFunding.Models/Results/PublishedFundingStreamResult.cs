using CalculateFunding.Models.Specs;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Results
{
    public class PublishedFundingStreamResult
    {
        [JsonProperty("fundingStream")]
        public FundingStream FundingStream { get; set; }

        [JsonProperty("allocationLineResult")]
        public PublishedAllocationLineResult AllocationLineResult { get; set; }

        [JsonProperty("fundingStreamPeriod")]
        public string FundingStreamPeriod { get; set; }

        [JsonProperty("distributionPeriod")]
        public string DistributionPeriod { get; set; }
    }
}
