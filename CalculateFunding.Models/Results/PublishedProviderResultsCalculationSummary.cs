using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class PublishedProviderResultsCalculationSummary
    {
        [JsonProperty("calculationName")]
        public string Name { get; set; }

        [JsonProperty("calculationVersionNumber")]
        public int Version { get; set; }

        [JsonProperty("calculationType")]
        public PublishedCalculationType CalculationType { get; set; }

        [JsonProperty("calculationAmount")]
        public decimal Amount { get; set; }
    }
}
