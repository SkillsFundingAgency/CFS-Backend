using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class DefinitionSpecificationRelationship : Reference
    {
        [JsonProperty("datasetId")]
        public string DatasetId { get; set; }

        [JsonProperty("current")]
        public DefinitionSpecificationRelationshipVersion Current { get; set; }
    }
}
