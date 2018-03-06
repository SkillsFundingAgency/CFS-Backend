using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class ProviderSourceDataset : VersionContainer<SourceDataset>
    {
        [JsonProperty("specification")]
        public Reference Specification { get; set; }

        [JsonProperty("provider")]
        public Reference Provider { get; set; }

        [JsonProperty("dataDefinition")]
        public VersionReference DataDefinition { get; set; }

        [JsonProperty("dataRelationship")]
        public VersionReference DataRelationship { get; set; }

        [JsonProperty("definesScope")]
        public bool DefinesScope { get; set; }
    }
}