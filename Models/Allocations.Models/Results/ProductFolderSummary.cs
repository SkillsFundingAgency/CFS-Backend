using System.Collections.Generic;
using Allocations.Models.Specs;
using Newtonsoft.Json;

namespace Allocations.Models.Results
{
    public class ProductFolderSummary : ResultSummary
    {

        public ProductFolderSummary(string id, string name, List<ProductSummary> products )
        {
            Id = id;
            Name = name;
            this.Products = products;
        }

        [JsonProperty("id")]
        public string Id { get; }
        [JsonProperty("name")]
        public string Name { get; }
        [JsonProperty("products")]
        public List<ProductSummary> Products { get; set; }
    }
}