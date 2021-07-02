using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Calcs
{
    public class DatasetRelationshipSummary : Reference
    {
        [JsonProperty("relationship")]
        public Reference Relationship { get; set; }

        [JsonProperty("datasetDefinition")]
        public DatasetDefinition DatasetDefinition { get; set; }

        [JsonProperty("datasetDefinitionId")]
        public string DatasetDefinitionId { get; set; }

        [JsonProperty("dataGranularity")]
        public DataGranularity DataGranularity { get; set; }

        [JsonProperty("definesScope")]
        public bool DefinesScope { get; set; }

        [JsonProperty("datasetId")]
        public string DatasetId { get; set; }

        [JsonProperty("datasetName")]
        public string DatasetName { get; set; }

        [JsonProperty("relationshipType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DatasetRelationshipType RelationshipType { get; set; }

        [JsonProperty("publishedSpecificationConfiguration")]
        public PublishedSpecificationConfiguration PublishedSpecificationConfiguration { get; set; }
    }
}