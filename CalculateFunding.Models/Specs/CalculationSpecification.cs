using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class CalculationSpecification : Reference
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }
    }
}