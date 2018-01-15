using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class Calculation : Reference
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }
    }
}