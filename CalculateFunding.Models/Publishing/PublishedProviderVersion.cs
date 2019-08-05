using System.Collections.Generic;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    /// <summary>
    /// A version of PublishedProvider. Used to track these properties over time each time an update is made from refresh, approve or publish funding.
    /// </summary>
    public class PublishedProviderVersion : VersionedItem
    {
        /// <summary>
        /// Cosmos ID for the document. This will be used as the document ID when saving to cosmos
        /// </summary>
        [JsonProperty("id")]
        public override string Id => $"publishedprovider-{FundingStreamId}-{FundingPeriodId}-{ProviderId}-{Version}";

        /// <summary>
        /// Funding Stream ID. eg PSG, DSG
        /// </summary>
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        /// <summary>
        /// Funding Period ID - Will be in the format of Funding Period Type Id-Funding Period eg AY-1920 or FY-2021
        /// </summary>
        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }

        /// <summary>
        /// Specification this ID is associated with
        /// </summary>
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        /// <summary>
        /// Entity ID for cosmos versioning. This refers to the parent PublishedProvider cosmos ID
        /// </summary>
        [JsonProperty("entityId")]
        public override string EntityId
        {
            get { return $"publishedprovider-{FundingStreamId}-{FundingPeriodId}-{ProviderId}-{Version}"; }
        }

        /// <summary>
        /// Published Provider Approval Status
        /// </summary>
        [JsonProperty("status")]
        public PublishedProviderStatus Status { get; set; }

        /// <summary>
        /// Provider ID (UKPRN) - eg 10001002
        /// </summary>
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        /// <summary>
        /// Partition key for cosmos - used the the publishedprovider collection. This document should be kept in the same paritition as the parent PublishedProvider, so will match the parent PublishedProvider comos ID.
        /// </summary>
        [JsonProperty("partitionKey")]
        public string PartitionKey => $"publishedprovider-{FundingStreamId}-{FundingPeriodId}-{ProviderId}";

        /// <summary>
        /// Payment Funding Lines - used to store the profiling result and total for all payment funding lines.
        /// The total funding per funding line and distribution periods are stored here.
        /// </summary>
        [JsonProperty("paymentFundingLines")]
        public IEnumerable<FundingLine> PaymentFundingLines { get; set; }

        /// <summary>
        /// Total funding for this provider
        /// </summary>
        [JsonProperty("totalFunding")]
        public decimal TotalFunding { get; set; }

        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<PublishedProviderVersion>(json);
        }
    }
}
