using Newtonsoft.Json;

namespace CalculateFunding.Models.Graph
{
    public abstract class BaseNode
    {
        [JsonProperty("partitionkey")]
        public abstract string PartitionKey { get; }
    }
}
