using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class Dataset : Reference
    {
        [JsonProperty("current")]
        public DatasetVersion Current { get; set; }

        [JsonProperty("definition")]
        public DatasetDefinitionVersion Definition { get; set; }

        [JsonProperty("relationshipId")]
        public string RelationshipId { get; set; }
    }
}