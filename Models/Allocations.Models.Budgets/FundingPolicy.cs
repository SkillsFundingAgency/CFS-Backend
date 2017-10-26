using System;
using Allocations.Repository;
using Newtonsoft.Json;

namespace Allocations.Models.Budgets
{
    public class FundingPolicy
    {
        [JsonProperty("id")]
        public  string Id => $"{Name}".ToSlug();
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("allocationLines")]
        public AllocationLine[] AllocationLines { get; set; }
    }
}