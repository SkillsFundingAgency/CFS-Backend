using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Graph
{
    public class DatasetField
    {
        public const string IdField = "datasetfieldid";

        [JsonProperty("datasetfieldrelatioshipname")]
        public string DatasetFieldRelatioshipName { get; set; }
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
        [JsonProperty("datasetfieldname")]
        public string DatasetFieldName { get; set; }
        [JsonProperty(IdField)]
        public string DatasetFieldId { get; set; }
        [JsonProperty("datasetfieldisaggregable")]
        public bool DatasetFieldIsAggregable { get; set; }

        protected bool Equals(DatasetField other)
        {
            return string.Equals(DatasetFieldName, other.DatasetFieldName, StringComparison.InvariantCultureIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DatasetField)obj);
        }

        public override int GetHashCode()
        {
            return DatasetFieldName.GetHashCode();
        }
    }
}
