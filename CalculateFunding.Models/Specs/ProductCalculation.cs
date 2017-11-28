using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class ProductCalculation
    {
        [JsonProperty("sourceCode")]
        public string SourceCode { get; set; }
    }
}