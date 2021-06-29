using CalculateFunding.Common.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace CalculateFunding.Models.Datasets
{
    public class OldDefinitionSpecificationRelationshipContent
    {
        [JsonProperty("current")]
        public DefinitionSpecificationRelationshipVersion Current { get; set; }

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
    }
}
