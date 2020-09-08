using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using CalculateFunding.Common.Models;
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
        /// Logical ID for this published provider to identify it between datastores and consistent between versions
        /// </summary>
        [JsonProperty("publishedProviderId")]
        public string PublishedProviderId => $"{FundingStreamId}-{FundingPeriodId}-{ProviderId}";

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
        /// The none default profiling patterns used for this provider
        /// in this period and funding stream keyed by funding line
        /// </summary>
        [JsonProperty("profilePatternKeys")]
        public ICollection<ProfilePatternKey> ProfilePatternKeys { get; set; }

        /// <summary>
        /// The custom profile periods used for this provider
        /// in this period and funding stream keyed by funding line
        /// </summary>
        [JsonProperty("customProfiles")]
        public IEnumerable<FundingLineProfileOverrides> CustomProfiles { get; set; }

        /// <summary>
        /// Flag indicating whether this provider has any custom profiles 
        /// </summary>
        [JsonProperty("hasCustomProfiles")]
        public bool HasCustomProfiles => CustomProfiles?.Any() == true;

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
        /// Calculations - used to store all calculations.
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

        public void AddVariationReasons(params VariationReason[] variationReasons) => VariationReasons = (VariationReasons ?? Array.Empty<VariationReason>())
                .Concat(variationReasons ?? Array.Empty<VariationReason>())
                .Distinct()
                .ToArray();

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
        /// Collection of carry over payments keyed by funding line for the funding period
        /// this published provider version is in
        /// </summary>
        [JsonProperty("carryOvers")]
        public ICollection<ProfilingCarryOver> CarryOvers { get; set; }

        /// <summary>
        /// Collection of profiling audits for each funding line profile updates
        /// </summary>
        [JsonProperty("profilingAudits")]
        public ICollection<ProfilingAudit> ProfilingAudits { get; set; }

        public decimal? GetCarryOverTotalForFundingLine(string fundingLineCode)
            => CarryOvers?.Where(_ => _.FundingLineCode == fundingLineCode).Sum(_ => _.Amount);

        public decimal? GetFundingLineTotal(string fundingLineCode)
            => FundingLines?.FirstOrDefault(_ => _.FundingLineCode == fundingLineCode)?.Value;

        public ProfilingAudit GetLatestFundingLineAudit(string fundingLineCode)
            => ProfilingAudits?.Where(_ => _.FundingLineCode == fundingLineCode)?.OrderByDescending(_ => _.Date)?.FirstOrDefault();

        public void AddCarryOver(string fundingLineCode,
            ProfilingCarryOverType type,
            decimal amount)
        {
            if (type == ProfilingCarryOverType.Undefined)
            {
                throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported {nameof(ProfilingCarryOverType)}");
            }

            if (string.IsNullOrWhiteSpace(fundingLineCode))
            {
                throw new ArgumentOutOfRangeException(nameof(fundingLineCode), fundingLineCode, "Funding Line Id cannot be missing");
            }

            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Carry overs must be greater than zero");
            }

            CarryOvers ??= new List<ProfilingCarryOver>();
            CarryOvers.Add(new ProfilingCarryOver
            {
                FundingLineCode = fundingLineCode,
                Type = type,
                Amount = amount
            });
        }

        public void AddProfilingAudit(string fundingLineCode, Reference user)
        {
            if (string.IsNullOrWhiteSpace(fundingLineCode))
            {
                throw new ArgumentOutOfRangeException(nameof(fundingLineCode), fundingLineCode, "Funding Line Id cannot be null or empty");
            }

            if (user == null)
            {
                throw new ArgumentOutOfRangeException(nameof(user), user, "Audit user cannot be null");
            }

            if (ProfilingAudits == null)
            {
                ProfilingAudits = new List<ProfilingAudit>();
            }

            ProfilingAudit profilingAudit = ProfilingAudits.SingleOrDefault(a => a.FundingLineCode == fundingLineCode);

            if (profilingAudit == null)
            {
                ProfilingAudits.Add(new ProfilingAudit() { FundingLineCode = fundingLineCode, User = user, Date = DateTime.Now.ToLocalTime() });
            }
            else
            {
                profilingAudit.User = user;
                profilingAudit.Date = DateTime.Now.ToLocalTime();
            }
        }

        [JsonIgnore]
        public bool HasResults => Calculations?.Any() == true;

        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<PublishedProviderVersion>(json);
        }

        public override bool Equals(object obj)
        {
            return GetHashCode().Equals(obj?.GetHashCode());
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                FundingStreamId,
                FundingPeriodId,
                SpecificationId,
                TemplateVersion,
                Status,
                ProviderId,
                MajorVersion,
                MinorVersion);
        }
    }
}
