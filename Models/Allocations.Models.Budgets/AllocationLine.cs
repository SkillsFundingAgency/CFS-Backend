using Allocations.Repository;
using Newtonsoft.Json;

namespace Allocations.Models.Budgets
{
    public class AllocationLine
    {
        [JsonProperty("id")]
        public string Id => $"{Name}".ToSlug();
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("productFolders")]
        public ProductFolder[] ProductFolders { get; set; }
    }
}