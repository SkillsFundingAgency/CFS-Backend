using Allocations.Models.Specs;
using Newtonsoft.Json;

namespace Allocations.Models.Results
{
    public class ProviderResult : DocumentEntity
    {
        public override string Id => $"{DocumentType}-{Budget.Id}-{Provider.Id}".ToSlug();

        [JsonProperty("budget")]
        public Reference Budget { get; set; }
        [JsonProperty("provider")]
        public Reference Provider { get; set; }

        [JsonProperty("sourceDatasets")]
        public object[] SourceDatasets { get; set; }

        [JsonProperty("products")]
        public ProductResult[] ProductResults { get; set; }
    }

    public class ProductResult 
    {

        [JsonProperty("fundingPolicy")]
        public Reference FundingPolicy { get; set; }
        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }
        [JsonProperty("productFolder")]
        public Reference ProductFolder { get; set; }
        [JsonProperty("product")]
        public Product Product { get; set; }
        [JsonProperty("value")]
        public decimal? Value { get; set; }

    }

}