using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Datasets
{
    public class DatasetAggregations : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return $"{SpecificationId}_{DatasetRelationshipId}";
            }
        }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("datasetRelationshipId")]
        public string DatasetRelationshipId { get; set; }

        [JsonProperty("fields")]
        public IEnumerable<AggregatedField> Fields { get; set; }
    }
}
