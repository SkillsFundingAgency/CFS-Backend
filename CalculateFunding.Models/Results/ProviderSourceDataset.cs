using CalculateFunding.Models.Datasets.Schema;
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
        public Reference DataDefinition { get; set; }

        [JsonProperty("dataRelationship")]
        public Reference DataRelationship { get; set; }

        [JsonProperty("dataGranularity")]
        public DataGranularity DataGranularity { get; set; }

        [JsonProperty("definesScope")]
        public bool DefinesScope { get; set; }
    }
}