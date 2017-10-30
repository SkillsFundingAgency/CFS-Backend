using System;
using Newtonsoft.Json;

namespace Allocations.Models.Results
{
    public class ProductTestScenarioResult : DocumentEntity
    {
        public override string Id => $"{DocumentType}-{UKBRN}-{Scenario}";

        [JsonProperty("ukBrn")]
        public DateTime UKBRN { get; set; }

        [JsonProperty("feature")]
        public string Feature { get; set; }

        [JsonProperty("scenario")]
        public string Scenario { get; set; }

        [JsonProperty("value")]
        public decimal Value { get; set; }

        [JsonProperty("hasPassed")]
        public bool HasPassed { get; set; }

        [JsonProperty("lastFailedDate")]
        public DateTime? LastFailedDate { get; set; }

        [JsonProperty("lastPassedDate")]
        public DateTime? LastPassedDate { get; set; }

        [JsonProperty("tags")]
        public string[] Tags { get; set; }

    }
}
