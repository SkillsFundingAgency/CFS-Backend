using System;
using CalculateFunding.Models.Calcs;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class FundingStreamCalculation
    {
        [JsonProperty("calculationName")]
        public string CalculationName { get; set; }

        [JsonProperty("calculationDisplayName")]
        public string CalculationDisplayName { get; set; }

        [JsonProperty("calculationType")]
        public CalculationType CalculationType { get; set; }

        [JsonProperty("calculationValue")]
        public decimal CalculationValue { get; set; }

        [JsonProperty("policyId")]
        public string PolicyId { get; set; }

        [JsonProperty("associatedWithAllocation")]
        public bool AssociatedWithAllocation { get; set; }
    }
}
