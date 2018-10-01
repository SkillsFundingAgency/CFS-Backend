using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace CalculateFunding.Models.Results
{
    public class PublishedAllocationLineResultVersion : VersionedItem
    {
        [JsonProperty("id")]
        public override string Id
        {
            get { return $"{PublishedProviderResultId}_version_{Version}"; }
        }

        [JsonProperty("entityId")]
        public override string EntityId
        {
            get { return $"{PublishedProviderResultId}"; }
        }

        [JsonProperty("publishedProviderResultId")]
        public string PublishedProviderResultId { get; set; }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("status")]
        public AllocationLineStatus Status { get; set; }

        [JsonProperty("value")]
        public decimal? Value { get; set; }

        [JsonProperty("provider")]
        public ProviderSummary Provider { get; set; }

        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<PublishedAllocationLineResultVersion>(json);
        }
    }
}
