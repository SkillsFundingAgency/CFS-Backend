using Newtonsoft.Json;
using System;

namespace CalculateFunding.Models.Results
{
    public class PublishedProviderCalculationResultCalculationVersion
    {
        [JsonProperty("version")]
        public int Version { get; set; }

        [JsonProperty("date")]
        public DateTimeOffset Date { get; set; }

        [JsonProperty("author")]
        public Reference Author { get; set; }

        [JsonProperty("comment")]
        public string Commment { get; set; }

        [JsonProperty("value")]
        public decimal? Value { get; set; }

        [JsonProperty("calculationType")]
        public PublishedCalculationType CalculationType { get; set; }

        [JsonProperty("provider")]
        public ProviderSummary Provider { get; set; }

        public PublishedProviderCalculationResultCalculationVersion Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<PublishedProviderCalculationResultCalculationVersion>(json);
        }
    }

}
