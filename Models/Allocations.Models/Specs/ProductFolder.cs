using System.Collections.Generic;
using Newtonsoft.Json;

namespace Allocations.Models.Specs
{
    public class ProductFolder
    {
        [JsonProperty("id")]
        public string Id => $"{Name}".ToSlug();

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("products")]
        public List<Product> Products { get; set; }
    }
}