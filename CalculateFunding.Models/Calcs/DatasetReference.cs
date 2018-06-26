using CalculateFunding.Models.Datasets.Schema;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class DatasetRelationshipSummary : Reference
    {
        [JsonProperty("relationship")]
        public Reference Relationship { get; set; }

        [JsonProperty("datasetDefinition")]
        public DatasetDefinition DatasetDefinition { get; set; }

        [JsonProperty("dataGranularity")]
        public DataGranularity DataGranularity { get; set; }

        [JsonProperty("definesScope")]
        public bool DefinesScope { get; set; }

        [JsonProperty("datasetId")]
        public string DatasetId { get; set; }
    }
}