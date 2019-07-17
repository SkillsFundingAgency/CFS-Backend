using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Publishing
{
    [Serializable]
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

        [JsonProperty("exceptionType")]
        public string ExceptionType { get; set; }

        [JsonProperty("exceptionMessage")]
        public string ExceptionMessage { get; set; }

        /// <summary>
        /// Elapsed time, used for debugging locally and shouldn't be stored in cosmos
        /// </summary>
        [JsonIgnore]
        [JsonProperty("elapsedTime")]
        public long ElapsedTime { get; set; }

        [JsonProperty("calculationType")]
        public CalculationType CalculationType { get; set; }

        [JsonProperty("version")]
        public int Version { get; set; }
    }
}
