using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class AllocationLineResult
    {
        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }

        [JsonProperty("value")]
        public decimal? Value { get; set; }
    }
}