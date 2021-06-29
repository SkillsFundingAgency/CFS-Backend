using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class DefinitionSpecificationRelationship : Reference
    {
        [JsonProperty("current")]
        public DefinitionSpecificationRelationshipVersion Current { get; set; }
    }
}
