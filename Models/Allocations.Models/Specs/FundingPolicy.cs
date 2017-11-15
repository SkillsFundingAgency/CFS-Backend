using System.Collections.Generic;
using Newtonsoft.Json;

namespace Allocations.Models.Specs
{
    public class FundingPolicy
    {
        [JsonProperty("id")]
        public  string Id => $"{Name}".ToSlug();
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("allocationLines")]
        public List<AllocationLine> AllocationLines { get; set; }
    }
}