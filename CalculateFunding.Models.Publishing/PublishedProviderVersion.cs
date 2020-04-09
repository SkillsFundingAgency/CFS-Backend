using System;
using System.Collections.Generic;
using System.Linq;
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
        public override string Id => $"publishedprovider-{ProviderId}-{FundingPeriodId}-{FundingStreamId}-{Version}";

        [JsonProperty("fundingId")]
        public string FundingId => $"{FundingStreamId}-{FundingPeriodId}-{ProviderId}-{MajorVersion}_{MinorVersion}";

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
        /// The custom profiling patterns used for this provider
        /// in this period and funding stream keyed by funding line
        /// </summary>
        [JsonProperty("profilePatternKey")]
        public ICollection<ProfilePatternKey> ProfilePatternKeys { get; set; }
        

        /// <summary>
        /// Specification this ID is associated with
        /// </summary>
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("templateVersion")]
        public string TemplateVersion { get; set; }

        /// <summary>
        /// Entity ID for cosmos versioning. This refers to the parent PublishedProvider cosmos ID
        /// </summary>
        [JsonProperty("entityId")]
        public override string EntityId => $"publishedprovider-{ProviderId}-{FundingPeriodId}-{FundingStreamId}";

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
        public string PartitionKey => $"publishedprovider-{ProviderId}-{FundingPeriodId}-{FundingStreamId}";

        /// <summary>
        /// Funding Lines - used to store the profiling result and total for all funding lines.
        /// The total funding per funding line and distribution periods are stored here.
        /// This will be consumed from the organisation group aggregator and variations over time.
        /// </summary>
        [JsonProperty("fundingLines")]
        public IEnumerable<FundingLine> FundingLines { get; set; }

        /// <summary>
        /// Calculations - used to store all calculatins.
        /// </summary>
        [JsonProperty("calculations")]
        public IEnumerable<FundingCalculation> Calculations { get; set; }

        /// <summary>
        /// Reference data that make up data for calculations.
        /// </summary>
        [JsonProperty("referenceData")]
        public IEnumerable<FundingReferenceData> ReferenceData { get; set; }

        /// <summary>
        /// Total funding for this provider in pounds and pence
        /// </summary>
        [JsonProperty("totalFunding")]
        public decimal? TotalFunding { get; set; }

        /// <summary>
        /// Major version
        /// </summary>
        [JsonProperty("majorVersion")]
        public int MajorVersion { get; set; }

        /// <summary>
        /// Minor version
        /// </summary>
        [JsonProperty("minorVersion")]
        public int MinorVersion { get; set; }

        /// <summary>
        /// Provider information
        /// </summary>
        [JsonProperty("provider")]
        public Provider Provider { get; set; }

        /// <summary>
        /// Provider IDs of Predecessor providers
        /// </summary>
        [JsonProperty("predecessors")]
        public ICollection<string> Predecessors { get; set; }

        /// <summary>
        /// Variation reasons
        /// </summary>
        [JsonProperty("variationReasons")]
        public IEnumerable<VariationReason> VariationReasons { get; set; }
        
        /// <summary>
        /// Errors blocking the release of this funding encountered
        /// during the publishing cycle that created this version
        /// </summary>
        [JsonProperty("errors")]
        public List<PublishedProviderError> Errors { get; set; }

        public void AddErrors(IEnumerable<PublishedProviderError> errors)
        {
            Errors ??= new List<PublishedProviderError>();
            Errors.AddRange(errors);
        }

        public void ResetErrors()
        {
            Errors?.Clear();
        }

        public bool HasErrors => Errors?.Any() == true;

        /// <summary>
        /// Job ID this PublishedProvider was updated or created on
        /// </summary>
        [JsonProperty("jobId")]
        public string JobId { get; set; }

        /// <summary>
        /// Correlation ID this PublishedProvider was updated or created on.
        /// This should line up with Provider variations and all updated made in a single refresh/approve/publish should have the same id
        /// </summary>
        [JsonProperty("correlationId")]
        public string CorrelationId { get; set; }
        
        /// <summary>
        /// Collection of any over payments keyed by funding line for the funding period
        /// this published provider version is in
        /// </summary>
        [JsonProperty("fundingLineOverPayments")]
        public IDictionary<string, decimal> FundingLineOverPayments { get; set; }

        public void AddFundingLineOverPayment(string fundingLineId, decimal overpayment)
        {
            if (string.IsNullOrWhiteSpace(fundingLineId))
            {
                throw new ArgumentOutOfRangeException(nameof(fundingLineId), fundingLineId, "Funding Line Id cannot be missing");
            }
            
            if (overpayment <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(overpayment), overpayment, "Over payments must be greater than zero");
            }

            FundingLineOverPayments ??= new Dictionary<string, decimal>();
            FundingLineOverPayments[fundingLineId] = overpayment;
        }
        
        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<PublishedProviderVersion>(json);
        }
    }
}
