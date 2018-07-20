using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Results
{
    public class PublishedFundingStreamResult
    {
        [JsonProperty("fundingStream")]
        public Reference FundingStream { get; set; }

        [JsonProperty("allocationLineResult")]
        public PublishedAllocationLineResult AllocationLineResult { get; set; }
    }
}
