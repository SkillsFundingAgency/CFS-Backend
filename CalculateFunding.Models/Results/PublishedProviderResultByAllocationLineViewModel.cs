using System;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class PublishedProviderResultByAllocationLineViewModel
    {
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("providerName")]
        public string ProviderName { get; set; }

        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("providerType")]
        public string ProviderType { get; set; }

        [JsonProperty("ukprn")]
        public string Ukprn { get; set; }

        [JsonProperty("fundingStreamName")]
        public string FundingStreamName { get; set; }

        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("allocationLineId")]
        public string AllocationLineId { get; set; }

        [JsonProperty("allocationLineName")]
        public string AllocationLineName { get; set; }

        [JsonProperty("fundingAmount")]
        public decimal? FundingAmount { get; set; }

        [JsonProperty("status")]
        public AllocationLineStatus Status { get; set; }

        [JsonProperty("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }

        [JsonProperty("authority")]
        public string Authority { get; set; }

        [JsonProperty("versionNumber")]
        public string VersionNumber { get; set; }
    }
}
