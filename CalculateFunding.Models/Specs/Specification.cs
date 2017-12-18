using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class Specification : Reference
    {

        [JsonProperty("academicYear")]
        public Reference AcademicYear { get; set; }

        [JsonProperty("fundingStream")]
        public Reference FundingStream { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("policies")]
        public List<PolicySpecification> Policies { get; set; }

    }
}