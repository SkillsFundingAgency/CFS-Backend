using System;
using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class CalculationResult
    {
        [JsonProperty("calculation")]
        public Reference Calculation { get; set; }

        [JsonProperty("calculationSpecification")]
        public Reference CalculationSpecification { get; set; }

        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }

        [JsonProperty("policySpecifications")]
        public List<Reference> PolicySpecifications { get; set; }

        [JsonProperty("value")]
        public decimal? Value { get; set; }

        [JsonProperty("exception")]
        public Exception Exception { get; set; }

        [JsonProperty("calculationType")]
        public CalculationType CalculationType { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }

    }
}