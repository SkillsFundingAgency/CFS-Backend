using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class PublishedProviderCalculationResultSummary
    {
        [JsonProperty("calculationName")]
        public string CalculationName { get; set; }

        [JsonProperty("calculationDisplayName")]
        public string CalculationDisplayName { get; set; }

        [JsonProperty("calculationVersion")]
        public int CalculationVersion{ get; set; }

        [JsonProperty("calculationType")]
        public string CalculationType { get; set; }

        [JsonProperty("calculationAmount")]
        public decimal? CalculationAmount { get; set; }

        [JsonProperty("allocationLineId")]
        public string AllocationLineId { get; set; }
    }
}
