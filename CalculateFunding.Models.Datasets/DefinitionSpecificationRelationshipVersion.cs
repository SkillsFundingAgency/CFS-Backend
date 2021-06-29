using System;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CalculateFunding.Models.Datasets
{
    public class DefinitionSpecificationRelationshipVersion : VersionedItem
    {
        [JsonProperty("id")]
        public override string Id => $"{RelationshipId}_version_{Version}";

        [JsonProperty("entityId")]
        public override string EntityId => $"{RelationshipId}";

        [JsonProperty("relationshipId")]
        public string RelationshipId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("datasetDefinition")]
        public Reference DatasetDefinition { get; set; }

        [JsonProperty("specification")]
        public Reference Specification { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("datasetVersion")]
        public DatasetRelationshipVersion DatasetVersion { get; set; }

        [JsonProperty("isSetAsProviderData")]
        public bool IsSetAsProviderData { get; set; }

        [JsonProperty("converterEnabled")]
        public bool ConverterEnabled { get; set; }

        [JsonProperty("usedInDataAggregations")]
        public bool UsedInDataAggregations { get; set; }

        [JsonProperty("lastUpdated")]
        public DateTimeOffset? LastUpdated { get; set; }

        [JsonProperty("relationshipType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DatasetRelationshipType RelationshipType { get; set; }

        [JsonProperty("publishedSpecificationConfiguration")]
        public PublishedSpecificationConfiguration PublishedSpecificationConfiguration { get; set; }

        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<DefinitionSpecificationRelationshipVersion>(json);
        }
    }
}
