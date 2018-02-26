using System.Collections.Generic;
using CalculateFunding.Models.Results;
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
    }
}