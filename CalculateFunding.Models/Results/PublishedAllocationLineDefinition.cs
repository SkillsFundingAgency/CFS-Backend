using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class PublishedAllocationLineDefinition : Reference
    {
        [JsonProperty("fundingRoute")]
        public PublishedFundingRoute FundingRoute { get; set; }

        [JsonProperty("isContractRequired")]
        public bool IsContractRequired { get; set; }

        [JsonProperty("shortName")]
        public string ShortName { get; set; }
    }
}
