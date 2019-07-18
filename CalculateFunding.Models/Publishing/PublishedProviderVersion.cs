using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Publishing
{
    public class PublishedProviderVersion : VersionedItem
    {
        [JsonProperty("id")]
        public override string Id => $"publishedprovider_{ProviderId}_{FundingPeriodId}_{FundingStreamId}_{Version}";

        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }

        [JsonProperty("feedIndexId")]
        public string FeedIndexId { get; set; }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("entityId")]
        public override string EntityId
        {
            get { return $"publishedprovider_{ProviderId}_{FundingPeriodId}_{FundingStreamId}"; }
        }

        [JsonProperty("status")]
        public PublishedProviderStatus Status { get; set; }

        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("partitionKey")]
        public string PartitionKey => $"publishedprovider_{ProviderId}_{FundingPeriodId}_{FundingStreamId}";

        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<PublishedProviderVersion>(json);
        }
    }
}
