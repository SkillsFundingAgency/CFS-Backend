using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class CalculationSpecificationSummary
    {
        [JsonProperty("calculationSpecification")]
        public Reference CalculationSpecification { get; set; }

        [JsonProperty("calculationType")]
        public CalculationType CalculationType { get; set; }
    }
}