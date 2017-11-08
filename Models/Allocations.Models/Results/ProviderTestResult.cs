using Newtonsoft.Json;

namespace Allocations.Models.Results
{
    public class ProviderTestResult : DocumentEntity
    {
        public override string Id => $"{DocumentType}-{Budget.Id}-{Provider.Id}".ToSlug();

        [JsonProperty("budget")]
        public Reference Budget { get; set; }
        [JsonProperty("provider")]
        public Reference Provider { get; set; }

        [JsonProperty("scenarioResults")]
        public ProductTestScenarioResult[] ScenarioResults { get; set; }
    }
}