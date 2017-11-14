using Newtonsoft.Json;

namespace Allocations.Models.Datasets
{
    public class AggregateField
    {
        [JsonProperty("datasetId")]
        public string DatasetDefinitionId { get; set; }

        [JsonProperty("fieldId")]
        public string DatasetFieldDefinitionId { get; set; }

        [JsonProperty("min")]
        public string Min { get; set; }

        [JsonProperty("max")]
        public string Max { get; set; }

        [JsonProperty("average")]
        public string Average { get; set; }

        [JsonProperty("popular")]
        public string[] Popular { get; set; }

    }
}