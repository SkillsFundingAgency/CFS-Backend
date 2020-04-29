using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Graph
{
    public class DataField
    {
        public const string IdField = "datafieldid";

        [JsonProperty("datafieldrelationshipname")]
        public string DataFieldRelationshipName { get; set; }
        [JsonProperty("specificationid")]
        public string SpecificationId { get; set; }

        [JsonProperty("calculationid")]
        public string CalculationId { get; set; }

        [JsonProperty("propertyname")]
        public string PropertyName { get; set; }

        [JsonProperty("datasetrelationshipid")]
        public string DatasetRelationshipId { get; set; }
        [JsonProperty("schemaid")]
        public string SchemaId { get; set; }
        [JsonProperty("schemafieldid")]
        public string SchemaFieldId { get; set; }
        [JsonProperty("datafieldname")]
        public string DataFieldName { get; set; }
        [JsonProperty(IdField)]
        public string DataFieldId { get; set; }
        [JsonProperty("datafieldisaggregable")]
        public bool DataFieldIsAggregable { get; set; }

        protected bool Equals(DataField other)
        {
            return string.Equals(DataFieldName, other.DataFieldName, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DataField)obj);
        }

        public override int GetHashCode()
        {
            return DataFieldName.GetHashCode();
        }
    }
}
