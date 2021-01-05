using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Core.Extensions;
using Microsoft.Extensions.Primitives;
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

        public Reference GetLatestFundingLineUser(string fundingLineCode)
            => GetLatestFundingLineAudit(fundingLineCode)?.User ?? Author;

        public DateTimeOffset GetLatestFundingLineDate(string fundingLineCode)
         => GetLatestFundingLineAudit(fundingLineCode)?.Date ?? Date;

        public bool FundingLineHasCustomProfile(string fundingLineCode)
            => CustomProfiles?.Any(_ => _.FundingLineCode == fundingLineCode) == true;

        public void AddCarryOver(string fundingLineCode,
            ProfilingCarryOverType type,
            decimal amount)
        {
            Guard.IsNullOrWhiteSpace(fundingLineCode, nameof(fundingLineCode), "Funding Line Id cannot be missing");
            Guard.Ensure(type != ProfilingCarryOverType.Undefined, $"Unsupported {nameof(ProfilingCarryOverType)}");
            Guard.Ensure(amount > 0, "Carry overs must be greater than zero");

            CarryOvers ??= new List<ProfilingCarryOver>();
            CarryOvers.Add(new ProfilingCarryOver
            {
                FundingLineCode = fundingLineCode,
                Type = type,
                Amount = amount
            });
        }

        public void RemoveCarryOver(string fundingLineCode)
        {
            Guard.IsNullOrWhiteSpace(fundingLineCode, nameof(fundingLineCode), "Funding Line Id cannot be missing");

            CarryOvers ??= new List<ProfilingCarryOver>();
            CarryOvers = CarryOvers.Except(CarryOvers.Where(_ =>
                    _.FundingLineCode == fundingLineCode))
                .ToList();
        }

        public void AddProfilingAudit(string fundingLineCode,
            Reference user)
        {
            Guard.IsNullOrWhiteSpace(fundingLineCode, nameof(fundingLineCode));
            Guard.ArgumentNotNull(user, nameof(user));

            ProfilingAudits ??= new List<ProfilingAudit>();

            ProfilingAudit profilingAudit = ProfilingAudits.SingleOrDefault(_ => _.FundingLineCode == fundingLineCode);

            if (profilingAudit == null)
            {
                ProfilingAudits.Add(new ProfilingAudit
                {
                    FundingLineCode = fundingLineCode,
                    User = user,
                    Date = DateTime.UtcNow
                });
            }
            else
            {
                profilingAudit.User = user;
                profilingAudit.Date = DateTime.UtcNow;
            }
        }

        public void AddOrUpdateCustomProfile(string fundingLineCode,
            decimal? carryOver,
            string distributionPeriodId)
        {
            DistributionPeriod distributionPeriod = GetDistributionPeriod(fundingLineCode, distributionPeriodId);

            CustomProfiles ??= new List<FundingLineProfileOverrides>();

            FundingLineProfileOverrides customProfile = CustomProfiles.SingleOrDefault(_ =>
                _.FundingLineCode == fundingLineCode) ?? new FundingLineProfileOverrides();

            customProfile.FundingLineCode = fundingLineCode;
            customProfile.CarryOver = carryOver;
            customProfile.DistributionPeriods ??= new List<DistributionPeriod>();

            DistributionPeriod existingDistributionPeriod = customProfile.DistributionPeriods
                .SingleOrDefault(_ => _.DistributionPeriodId == distributionPeriod.DistributionPeriodId);

            if (existingDistributionPeriod == null)
            {
                customProfile.DistributionPeriods = customProfile.DistributionPeriods.Concat(new[]
                {
                    distributionPeriod
                });
                CustomProfiles = CustomProfiles.Concat(new[]
                {
                    customProfile
                });
            }
            else
            {
                existingDistributionPeriod.Value = distributionPeriod.Value;
                existingDistributionPeriod.ProfilePeriods = distributionPeriod.ProfilePeriods.DeepCopy();
            }
        }

        public void UpdateDistributionPeriodForFundingLine(string fundingLineCode,
            string distributionPeriodId,
            IEnumerable<ProfilePeriod> profilePeriods)
        {
            DistributionPeriod distributionPeriod = GetDistributionPeriod(fundingLineCode, distributionPeriodId);

            profilePeriods ??= ArraySegment<ProfilePeriod>.Empty;

            decimal sumTotalForDistributionPeriod = profilePeriods.Sum(_ => _.ProfiledValue);
            distributionPeriod.Value = sumTotalForDistributionPeriod;
            distributionPeriod.ProfilePeriods = profilePeriods.ToArray();
        }

        private DistributionPeriod GetDistributionPeriod(string fundingLineCode,
            string distributionPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingLineCode, nameof(fundingLineCode));
            Guard.IsNullOrWhiteSpace(distributionPeriodId, nameof(distributionPeriodId));

            FundingLine fundingLine = FundingLines.SingleOrDefault(fl => fl.FundingLineCode == fundingLineCode);

            Guard.Ensure(fundingLine != null, $"Did not locate a funding line with code {fundingLineCode}");

            DistributionPeriod distributionPeriod = fundingLine.DistributionPeriods?
                .SingleOrDefault(d => d.DistributionPeriodId == distributionPeriodId);

            Guard.Ensure(distributionPeriod != null, $"Distribution period {distributionPeriodId} not found for funding line {fundingLineCode}.");

            return distributionPeriod;
        }

        public void SetProfilePatternKey(ProfilePatternKey profilePatternKey,
            Reference author)
        {
            if (ProfilePatternKeys?.Any(_ => _.FundingLineCode == profilePatternKey.FundingLineCode) == true)
            {
                ProfilePatternKeys
                    .SingleOrDefault(_ => _.FundingLineCode == profilePatternKey.FundingLineCode)
                    .Key = profilePatternKey.Key;
            }
            else
            {
                ProfilePatternKeys ??= new List<ProfilePatternKey>();
                ProfilePatternKeys.Add(profilePatternKey);
            }

            AddProfilingAudit(profilePatternKey.FundingLineCode, author);
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
