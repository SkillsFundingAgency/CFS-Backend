using System.Collections.Generic;
using CalculateFunding.Models.Results;
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

        [JsonProperty("policies")]
        public List<Reference> Policies { get; set; }

        [JsonProperty("specification")]
        public SpecificationSummary Specification { get; set; }

        [JsonProperty("period")]
        public Reference Period { get; set; }

        [JsonProperty("fundingStream")]
        public Reference FundingStream { get; set; }

        [JsonProperty("buildProjectId")]
        public string BuildProjectId { get; set; }

        [JsonProperty("caclulationType")]
        public CalculationType CalculationType { get; set; }

        /// <summary>
        /// Used for putting description in the built assembly, this gets populated only when being called from this scenario.
        /// This value shouldn't be stored in CosmosDB
        /// The same models are used for persistance and input to the calculation engine
        /// </summary>
        [JsonIgnore]
        public string Description { get; set; }
    }
}