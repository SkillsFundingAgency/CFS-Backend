using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Results
{
    public class PublishedFundingStreamResult
    {
        public PublishedFundingStreamResult()
        {
            AllocationLineResults = Enumerable.Empty<PublishedAllocationLineResult>();
        }

        [JsonProperty("fundingStream")]
        public Reference FundingStream { get; set; }

        [JsonProperty("allocationLineResults")]
        public IEnumerable<PublishedAllocationLineResult> AllocationLineResults { get; set; }
    }
}
