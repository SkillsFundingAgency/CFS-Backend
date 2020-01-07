using Newtonsoft.Json;

namespace CalculateFunding.Models.CosmosDbScaling
{
    public class CosmosDbScalingJobConfig
    {
        [JsonProperty("jobDefinitionId")]
        public string JobDefinitionId { get; set; }

        [JsonProperty("jobRequestUnits")]
        public int JobRequestUnits { get; set; }
    }
}
