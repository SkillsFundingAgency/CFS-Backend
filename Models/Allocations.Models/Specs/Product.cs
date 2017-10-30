using Newtonsoft.Json;

namespace Allocations.Models.Specs
{

    public class Product
    {
        [JsonProperty("id")]
        public string Id => $"{Name}".ToSlug();
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("featureFile")]
        public string FeatureFile { get; set; }
    }
}