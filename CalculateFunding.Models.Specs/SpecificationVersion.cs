using System;
using System.Collections.Generic;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Providers;
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

        [JsonProperty("templateIds")]
        public Dictionary<string, string> TemplateIds { get; set; } = new Dictionary<string, string>();

        [JsonProperty("externalPublicationDate")]
        public DateTimeOffset? ExternalPublicationDate { get; set; }

        [JsonProperty("earliestPaymentAvailableDate")]
        public DateTimeOffset? EarliestPaymentAvailableDate { get; set; }

        [JsonProperty("profileVariationPointers")]
        public IEnumerable<ProfileVariationPointer> ProfileVariationPointers { get; set; }
        
        [JsonProperty("providerSource")]
        public ProviderSource ProviderSource { get; set; }

        [JsonProperty("providerSnapshotId")]
        public int? ProviderSnapshotId { get; set; }

        [JsonProperty("coreProviderVersionUpdates")]
        public CoreProviderVersionUpdates CoreProviderVersionUpdates { get; set; }

        public void AddOrUpdateTemplateId(string fundingStreamId,
            string templateId)
        {
            if (TemplateIds.ContainsKey(fundingStreamId))
                TemplateIds[fundingStreamId] = templateId;
            else
                TemplateIds.Add(fundingStreamId, templateId);
        }

        public bool TemplateVersionHasChanged(string fundingStreamId,
            string templateId)
            => !TemplateIds.TryGetValue(fundingStreamId, out string currentTemplate) ||
               currentTemplate != templateId;

        public string GetTemplateVersionId(string fundingStreamId)
        {
            return TemplateIds.ContainsKey(fundingStreamId) ? TemplateIds[fundingStreamId] : null;
        }

        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<SpecificationVersion>(json);
        }
    }
}
