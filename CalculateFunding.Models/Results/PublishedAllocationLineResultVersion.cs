using System;
using System.Collections.Generic;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class PublishedAllocationLineResultVersion : VersionedItem
    {
        [JsonProperty("id")]
        public override string Id
        {
            get { return $"{PublishedProviderResultId}_version_{Version}"; }
        }

        [JsonProperty("feedIndexId")]
        public string FeedIndexId { get; set; }

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

        [JsonProperty("major")]

        public int Major { get; set; }

        [JsonProperty("minor")]

        public int Minor { get; set; }

        [JsonProperty("versionNumber")]
        public string VersionNumber
        {
            get
            {
                return $"{Major}.{Minor}";
            }
        }

        [JsonProperty("profilePeriods")]
        public IEnumerable<ProfilingPeriod> ProfilingPeriods { get; set; }

        [JsonProperty("financialEnvelopes")]
        public IEnumerable<FinancialEnvelope> FinancialEnvelopes { get; set; }

        [JsonProperty("calculations")]
        public IEnumerable<PublishedProviderCalculationResult> Calculations { get; set; }

        [JsonProperty("variationReasons")]
        public IEnumerable<VariationReason> VariationReasons { get; set; }

        [JsonProperty("jobId")]
        public string JobId { get; set; }

        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }

        [JsonProperty("predecessors")]
        public IEnumerable<string> Predecessors { get; set; }

        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<PublishedAllocationLineResultVersion>(json);
        }
    }
}
