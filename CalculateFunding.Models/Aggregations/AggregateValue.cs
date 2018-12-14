using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Aggregations
{
    public class AggregateValue
    {
        [JsonProperty("fieldType")]
        public AggregatedType AggregatedType { get; set; }

        [JsonProperty("value")]
        public decimal? Value { get; set; }

        [JsonProperty("fieldDefinitionName")]
        public string FieldDefinitionName { get; set; }

        [JsonIgnore]
        public string FieldReference
        {
            get
            {
                return $"{FieldDefinitionName}_{AggregatedType.ToString()}";
            }
        }
    }
}
