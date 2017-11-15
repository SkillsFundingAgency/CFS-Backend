using System.Collections.Generic;
using Newtonsoft.Json;

namespace Allocations.Models.Specs
{
    public class AllocationLine
    {
        [JsonProperty("id")]
        public string Id => $"{Name}".ToSlug();
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("productFolders")]
        public List<ProductFolder> ProductFolders { get; set; }
    }
}