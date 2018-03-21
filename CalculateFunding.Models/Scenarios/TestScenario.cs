using CalculateFunding.Models.Results;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Scenarios
{
    public class TestScenario : VersionContainer<TestScenarioVersion>
    {
        [JsonProperty("specification")]
        public SpecificationSummary Specification { get; set; }

        [JsonProperty("period")]
        public Reference Period { get; set; }

        [JsonProperty("fundingStream")]
        public Reference FundingStream { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }
    }
}