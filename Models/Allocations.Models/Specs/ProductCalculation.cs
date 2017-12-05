using Newtonsoft.Json;

namespace Allocations.Models.Specs
{
    public class ProductCalculation
    {
        [JsonProperty("sourceCode")]
        public string SourceCode { get; set; }
    }
}