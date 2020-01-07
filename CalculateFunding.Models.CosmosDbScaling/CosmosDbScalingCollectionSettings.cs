using System;
using Newtonsoft.Json;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Models.CosmosDbScaling
{
    public class CosmosDbScalingCollectionSettings : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id => CosmosCollectionType.ToString();

        [JsonProperty("cosmosCollectionType")]
        public CosmosCollectionType CosmosCollectionType { get; set; }

        [JsonProperty("lastScalingIncrementValue")]
        public int LastScalingIncrementValue { get; set; }

        [JsonProperty("lastScalingDecrementValue")]
        public int LastScalingDecrementValue { get; set; }

        [JsonProperty("lastScalingIncrementDateTime")]
        public DateTimeOffset? LastScalingIncrementDateTime { get; set; }

        [JsonProperty("lastScalingDeccrementDateTime")]
        public DateTimeOffset? LastScalingDecrementDateTime { get; set; }

        [JsonProperty("currentRequestUnits")]
        public int CurrentRequestUnits { get; set; }

        [JsonProperty("maxRequestUnits")]
        public int MaxRequestUnits { get; set; }

        [JsonProperty("minRequestUnits")]
        public int MinRequestUnits { get; set; }

        [JsonIgnore]
        public int AvailableRequestUnits => MaxRequestUnits - CurrentRequestUnits;

        [JsonIgnore]
        public bool IsAtBaseLine => CurrentRequestUnits == MinRequestUnits;
    }
}
