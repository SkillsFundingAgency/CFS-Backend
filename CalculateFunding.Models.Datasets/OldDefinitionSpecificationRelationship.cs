using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{

    public class OldDefinitionSpecificationRelationship : Reference
    {
        [JsonProperty("documentType")]
        public string DocumentType { get; set; }

        [JsonProperty("content")]
        public OldDefinitionSpecificationRelationshipContent Content { get; set; }
    }
}
