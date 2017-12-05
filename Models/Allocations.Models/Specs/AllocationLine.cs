using System.Collections.Generic;
using Allocations.Models.Results;
using Newtonsoft.Json;

namespace Allocations.Models.Specs
{
    public class AllocationLine : ResultSummary
    {
        [JsonProperty("id")]
        public string Id => $"{Name}".ToSlug();
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("productFolders")]
        public List<ProductFolder> ProductFolders { get; set; }
    }
}