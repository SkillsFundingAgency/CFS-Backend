using Newtonsoft.Json;

namespace CalculateFunding.Models.Graph
{
    public class DatasetRelationship : SpecificationNode
    {
        public const string IdField = "datasetRelationshipId";

        [JsonProperty("datasetRelationshipId")]
        public string DatasetRelationshipId { get; set; }

        [JsonProperty("datasetRelationshipName")]
        public string DatasetRelationshipName { get; set; }

        [JsonProperty("datasetRelationshipType")]
        public DatasetRelationshipType DatasetRelationshipType { get; set; }

        [JsonProperty("schemaName")]
        public string SchemaName { get; set; }

        [JsonProperty("schemaId")]
        public string SchemaId { get; set; }
    }
}
