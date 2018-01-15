using System.Collections.Generic;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class Calculation : VersionContainer<CalculationVersion>
    {
        [JsonProperty("calculationSpecification")]
        public Reference CalculationSpecification { get; set; }

        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }

        [JsonProperty("policySpecifications")]
        public List<Reference> PolicySpecifications { get; set; }
    }
}