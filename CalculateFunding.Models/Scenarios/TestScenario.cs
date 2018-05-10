using CalculateFunding.Models.Results;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Scenarios
{
    public class TestScenario : VersionContainer<TestScenarioVersion>
    {
        [JsonProperty("specification")]
        public SpecificationSummary Specification { get; set; }

        [JsonProperty("fundingPeriod")]
        public Reference FundingPeriod { get; set; }

        [JsonProperty("fundingStreams")]
        public IEnumerable<Reference> FundingStreams { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}