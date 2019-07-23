using System.Collections.Generic;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class Calculation : Reference
    {
        [JsonProperty("calculationSpecification")]
        public Reference CalculationSpecification { get; set; }

        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("fundingPeriod")]
        public Reference FundingPeriod { get; set; }

        [JsonProperty("fundingStream")]
        public Reference FundingStream { get; set; }

        [JsonProperty("buildProjectId")]
        public string BuildProjectId { get; set; }

        [JsonProperty("calculationType")]
        public CalculationType CalculationType { get; set; }

        /// <summary>
        /// Used for putting description in the built assembly, this gets populated only when being called from this scenario.
        /// This value shouldn't be stored in CosmosDB
        /// The same models are used for persistance and input to the calculation engine
        /// </summary>
        [JsonIgnore]
        public string Description { get; set; }

        [JsonProperty("current")]
        public CalculationVersion Current { get; set; }

        [JsonProperty("sourceCodeName")]
        public string SourceCodeName { get; set; }
    }
}