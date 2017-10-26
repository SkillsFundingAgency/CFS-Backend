using System;
using Allocations.Repository;
using Newtonsoft.Json;

namespace Allocations.Models.Budgets
{

    public class Product
    {
        [JsonProperty("id")]
        public string Id => $"{Name}".ToSlug();
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
    }
}