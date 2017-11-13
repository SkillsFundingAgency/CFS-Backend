using Newtonsoft.Json;

namespace Allocations.Functions.Results.Models
{
    public class AllocationLine : ResultSummary
    {
        [JsonProperty("id")]
        public string Id { get; }
        [JsonProperty("name")]
        public string Name { get; }

        public AllocationLine(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
