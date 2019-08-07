using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationVersion : VersionedItem
    {
        [JsonProperty("id")]
        public override string Id => $"{SpecificationId}_version_{Version}";

        [JsonProperty("entityId")]
        public override string EntityId => $"{SpecificationId}";

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("fundingPeriod")]
        public Reference FundingPeriod { get; set; }

        [JsonProperty("providerVersionId")]
        public string ProviderVersionId { get; set; }

        [JsonProperty("fundingStreams")]
        public IEnumerable<Reference> FundingStreams { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("dataDefinitionRelationshipIds")]
        public IEnumerable<string> DataDefinitionRelationshipIds { get; set; }

		[JsonProperty("variationDate")]
		public DateTimeOffset? VariationDate { get; set; }
        
        [JsonProperty("templateId")]
        public string TemplateId { get; set; }

        [JsonProperty("templateIds")]
        public Dictionary<string, string> TemplateIds { get; set; } = new Dictionary<string, string>();

        public void AddOrUpdateTemplateId(string fundingStreamId,
            string templateId)
        {
            if (TemplateIds.ContainsKey(fundingStreamId))
                TemplateIds[fundingStreamId] = templateId;
            else
                TemplateIds.Add(fundingStreamId, templateId);
        }

        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<SpecificationVersion>(json);
        }
    }
}
