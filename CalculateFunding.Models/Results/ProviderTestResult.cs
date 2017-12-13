using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class ProviderTestResult : Reference
    {

        [JsonProperty("budget")]
        public Reference Budget { get; set; }
        [JsonProperty("provider")]
        public Reference Provider { get; set; }

        [JsonProperty("scenarioResults")]
        public ProductTestScenarioResult[] ScenarioResults { get; set; }
    }
}