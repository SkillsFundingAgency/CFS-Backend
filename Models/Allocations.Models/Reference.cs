using Newtonsoft.Json;

namespace Allocations.Models
{
    public class Reference
    {
        [JsonProperty("id")]
        public string Id { get; }
        [JsonProperty("name")]
        public string Name { get; }

        public Reference(string id, string name)
        {
            Id = id;
            Name = name;
        }
    }
}
