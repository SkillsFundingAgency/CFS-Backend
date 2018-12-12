using System.Collections.Generic;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class PublishedFundingStreamVersion : VersionedItem
    {
        [JsonProperty("id")]
        public override string Id
        {
            get { return $"{PublishedFundingStreamId}_version_{Version}"; }
        }

        [JsonProperty("entityId")]
        public override string EntityId
        {
            get { return $"{PublishedFundingStreamId}"; }
        }

        [JsonProperty("publishedFundingStreamId")]
        public string PublishedFundingStreamId { get; set; }

        [JsonProperty("provider")]
        public ProviderSummary Provider { get; set; }

        [JsonProperty("providerId")]
        public string ProviderId { get { return Provider.Id; } }

        [JsonProperty("specification")]
        public VersionReference Specification { get; set; }

        [JsonProperty("fundingStream")]
        public FundingStreamSummary FundingStream { get; set; }

        [JsonProperty("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonProperty("allocations")]
        public IEnumerable<PublishedFundingStreamAllocationLine> Allocations { get; }

        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<PublishedFundingStreamVersion>(json);
        }
    }
}
