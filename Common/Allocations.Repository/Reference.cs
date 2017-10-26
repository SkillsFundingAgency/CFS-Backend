using Newtonsoft.Json;

namespace Allocations.Repository
{
    public class Reference
    {
        public Reference(string id, string name)
        {
            Id = id;
            Name = name;
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }
    }
}