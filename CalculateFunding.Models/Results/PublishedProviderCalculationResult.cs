using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class PublishedProviderCalculationResult
    {
        [JsonProperty("calculationSpecification")]
        public Reference CalculationSpecification { get; set; }

        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }

        [JsonProperty("status")]
        public PublishStatus Status { get; set; }

        [JsonProperty("value")]
        public decimal? Value { get; set; }

        [JsonProperty("calculationVersion")]
        public int CalculationVersion { get; set; }

        [JsonProperty("calculationType")]
        public PublishedCalculationType CalculationType { get; set; }
    }
}
