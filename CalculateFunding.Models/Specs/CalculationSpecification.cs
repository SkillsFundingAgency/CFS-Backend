using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class CalculationSpecification : Reference
    {
        [JsonProperty("description")]
        public string Description { get; set; }
    }
}