using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class CalculationImplementation
    {
        [JsonProperty("specification")]
        public Reference Specification { get; set; }
        [JsonProperty("calculationSpecification")]
        public Reference CalculationSpecification { get; set; }
        [JsonProperty("implementation")]
        public Reference Implementation { get; set; }
        [JsonProperty("sourceCode")]
        public string SourceCode { get; set; }
    }
}