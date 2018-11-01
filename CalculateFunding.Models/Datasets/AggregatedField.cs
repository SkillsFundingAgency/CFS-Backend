using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class AggregatedField
    {
        [JsonProperty("fieldType")]
        public AggregatedFieldType FieldType { get; set; }

        [JsonProperty("value")]
        public decimal? Value { get; set; }

        [JsonProperty("fieldDefinitionName")]
        public string FieldDefinitionName { get; set; }

        [JsonIgnore]
        public string FieldReference
        {
            get
            {
                return $"{FieldDefinitionName}_{FieldType.ToString()}";
            }
        }
    }
}
