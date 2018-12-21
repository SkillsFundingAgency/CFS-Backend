using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class AllocationLine : Reference
    {
        [JsonProperty("fundingRoute")]
        public FundingRoute FundingRoute { get; set; }

        [JsonProperty("isContractRequired")]
        public bool IsContractRequired { get; set; }

        [JsonProperty("shortName")]
        public string ShortName { get; set; }
    }
}