using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets.Schema
{
    public class FieldDefinition
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("identifierFieldType")]
        public IdentifierFieldType? IdentifierFieldType { get; set; }

        [JsonProperty("matchExpression")]
        public string MatchExpression { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("type")]
        public FieldType Type { get; set; }

        [JsonProperty("required")]
        public bool Required { get; set; }

        [JsonProperty("min")]
        public int? Minimum { get; set; }

        [JsonProperty("max")]
        public int? Maximum { get; set; }

        [JsonProperty("mustMatch")]
        public List<string> MustMatch { get; set; }

        [JsonProperty("isAggregable")]
        public bool IsAggregable { get; set; }

        [JsonIgnore]
        public bool IsNumeric
        {
            get
            {
                return Type == FieldType.Decimal || Type == FieldType.Float || Type == FieldType.Integer;
            }
        }
    }
}