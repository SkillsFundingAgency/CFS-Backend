using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Services.Core.Extensions;
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
        public IEnumerable<ProfilePatternKey> ProfilePatternKeys { get; set; }

        [JsonProperty("reProfileAudits")]
        public IEnumerable<ReProfileAudit> ReProfileAudits {
            get
            {
                return _reProfileAudit ??= new ConcurrentBag<ReProfileAudit>();
            }
            set
            {
                _reProfileAudit = new ConcurrentBag<ReProfileAudit>(value ?? ArraySegment<ReProfileAudit>.Empty);
            }
        }

        private ConcurrentBag<ReProfileAudit> _reProfileAudit;

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

        [JsonIgnore]
        [IgnoreDataMember]
        public IEnumerable<FundingLine> PaymentFundingLinesWithValues => ProfiledPaymentFundingLines?.Where(_ => _.Value.HasValue) ?? ArraySegment<FundingLine>.Empty;

        [JsonIgnore]
        [IgnoreDataMember]
        public IEnumerable<FundingLine> PaymentFundingLinesWithoutValues => ProfiledPaymentFundingLines?.Where(_ => !_.Value.HasValue) ?? ArraySegment<FundingLine>.Empty;

        [JsonIgnore]
        [IgnoreDataMember]
        public IEnumerable<FundingLine> CustomPaymentFundingLines => FundingLines?.Where(_ => _.Type == FundingLineType.Payment && FundingLineHasCustomProfile(_.FundingLineCode)) ?? ArraySegment<FundingLine>.Empty;

        [JsonIgnore]
        [IgnoreDataMember]
        public IEnumerable<FundingLine> ProfiledPaymentFundingLines => FundingLines?.Where(_ => _.Type == FundingLineType.Payment && !FundingLineHasCustomProfile(_.FundingLineCode)) ?? ArraySegment<FundingLine>.Empty;

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

        public void ResetErrors(Predicate<PublishedProviderError> predicate)
        {
            Errors?.RemoveAll(predicate);
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
        public IEnumerable<ProfilingAudit> ProfilingAudits { get; set; }
        
        /// <summary>
        /// Indicates whether the allocations for this version
        /// are indicative of payment if the provider converts
        /// </summary>
        [JsonProperty("isIndicative")]
        public bool IsIndicative { get; set; }

        public bool SetIsIndicative(HashSet<string> indicativeStatus)
        {
            string currentProviderStatus = Provider?.Status;

            // make sure we get the current flag here
            bool isIndicative = IsIndicative;

            IsIndicative = indicativeStatus.Contains(currentProviderStatus);

            // if the current indicative flag has been changed then return true
            return isIndicative != IsIndicative;
        }

        public bool IsConverter(DateTimeOffset? fundingPeriodStartDate,
            DateTimeOffset? fundingPeriodEndDate,
            string academyConverter)
        {
            return Provider != null && 
                Provider.ReasonEstablishmentOpened == academyConverter &&
                Provider.Predecessors.AnyWithNullCheck() &&
                Provider.DateOpened != null &&
                Provider.DateOpened.Value >= fundingPeriodStartDate &&
                Provider.DateOpened.Value <= fundingPeriodEndDate;
        }

        public decimal? GetCarryOverTotalForFundingLine(string fundingLineCode)
            => CarryOvers?.Where(_ => _.FundingLineCode == fundingLineCode).Sum(_ => _.Amount);

        public decimal? GetFundingLineTotal(string fundingLineCode)
            => FundingLines?.FirstOrDefault(_ => _.FundingLineCode == fundingLineCode)?.Value;

        public ProfilingAudit GetLatestFundingLineAudit(string fundingLineCode)
            => ProfilingAudits?.Where(_ => _.FundingLineCode == fundingLineCode).OrderByDescending(_ => _.Date).FirstOrDefault();

        public Reference GetLatestFundingLineUser(string fundingLineCode)
            => GetLatestFundingLineAudit(fundingLineCode)?.User ?? Author;

        public DateTimeOffset GetLatestFundingLineDate(string fundingLineCode)
         => GetLatestFundingLineAudit(fundingLineCode)?.Date ?? Date;

        public bool FundingLineHasCustomProfile(string fundingLineCode)
            => CustomProfiles?.Any(_ => _.FundingLineCode == fundingLineCode) == true;

        public IEnumerable<PublishedProviderError> GetErrorsForFundingLine(string fundingLineCode, string fundingStreamId)
            => Errors?.Where(_ => _.FundingLineCode == fundingLineCode && _.FundingStreamId == fundingStreamId);

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

            ProfilingAudits ??= ArraySegment<ProfilingAudit>.Empty;

            ProfilingAudit profilingAudit = ProfilingAudits.SingleOrDefault(_ => _.FundingLineCode == fundingLineCode);

            if (profilingAudit == null)
            {
                ProfilingAudits = ProfilingAudits.Concat(new[] {new ProfilingAudit
                    {
                        FundingLineCode = fundingLineCode,
                        User = user,
                        Date = DateTime.UtcNow
                    }
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

            CustomProfiles ??= ArraySegment<FundingLineProfileOverrides>.Empty;

            FundingLineProfileOverrides customProfile = CustomProfiles.SingleOrDefault(_ =>
                _.FundingLineCode == fundingLineCode);

            if (customProfile == null)
            {
                customProfile = new FundingLineProfileOverrides
                {
                    FundingLineCode = fundingLineCode,
                    DistributionPeriods = new List<DistributionPeriod>()
                };

                CustomProfiles = CustomProfiles.Concat(new[]
                {
                    customProfile
                });
            }

            customProfile.CarryOver = carryOver;

            DistributionPeriod existingDistributionPeriod = customProfile.DistributionPeriods
                .SingleOrDefault(_ => _.DistributionPeriodId == distributionPeriod.DistributionPeriodId);

            if (existingDistributionPeriod == null)
            {
                customProfile.DistributionPeriods = customProfile.DistributionPeriods.Concat(new[]
                {
                    distributionPeriod
                });
            }
            else
            {
                existingDistributionPeriod.Value = distributionPeriod.Value;
                existingDistributionPeriod.ProfilePeriods = distributionPeriod.ProfilePeriods.DeepCopy();
            }
        }

        public void VerifyProfileAmountsMatchFundingLineValue(string fundingLineCode, IEnumerable<ProfilePeriod> profilePeriods, decimal? carryOver)
        {
            decimal fundingLineTotal = GetFundingLineTotal(fundingLineCode).GetValueOrDefault();
            decimal reProfileFundingLineTotal = profilePeriods.Sum(_ => _.ProfiledValue);
            decimal totalAmount = reProfileFundingLineTotal + carryOver.GetValueOrDefault();

            if (totalAmount != fundingLineTotal)
            {
                throw new InvalidOperationException(
                    $"Profile amounts ({fundingLineTotal}) and carry over amount ({carryOver.GetValueOrDefault()}) does not equal funding line total requested ({reProfileFundingLineTotal}) from strategy.");
            }
        }

        public void UpdateDistributionPeriodForFundingLine(string fundingLineCode,
            string distributionPeriodId,
            IEnumerable<ProfilePeriod> profilePeriods,
            DistributionPeriod distributionPeriodToAdd = null)
        {
            DistributionPeriod distributionPeriod = GetDistributionPeriod(fundingLineCode, distributionPeriodId, distributionPeriodToAdd);

            profilePeriods ??= ArraySegment<ProfilePeriod>.Empty;

            decimal sumTotalForDistributionPeriod = profilePeriods.Sum(_ => _.ProfiledValue);
            distributionPeriod.Value = sumTotalForDistributionPeriod;
            distributionPeriod.ProfilePeriods = profilePeriods.ToArray();
        }

        private FundingLine GetFundingLine(string fundingLineCode)
        {
            FundingLine fundingLine = FundingLines.SingleOrDefault(fl => fl.FundingLineCode == fundingLineCode);
            
            Guard.Ensure(fundingLine != null, $"Did not locate a funding line with code {fundingLineCode}");

            return fundingLine;
        }

        private DistributionPeriod GetDistributionPeriod(string fundingLineCode,
            string distributionPeriodId,
            DistributionPeriod distributionPeriodToAdd = null)
        {
            Guard.IsNullOrWhiteSpace(fundingLineCode, nameof(fundingLineCode));
            Guard.IsNullOrWhiteSpace(distributionPeriodId, nameof(distributionPeriodId));

            FundingLine fundingLine = GetFundingLine(fundingLineCode);

            DistributionPeriod distributionPeriod = fundingLine.DistributionPeriods?.SingleOrDefault(d => d.DistributionPeriodId == distributionPeriodId);

            // only use the guard for custom profiling
            if (distributionPeriodToAdd == null)
            {
                Guard.Ensure(distributionPeriod != null, $"Distribution period {distributionPeriodId} not found for funding line {fundingLineCode}.");
            }
            else
            {
                // on re-profiling if the funding value is 0 then we won't currently have any distribution periods set against the refreshed provider
                // for this scenario we need to use what has come back from re-profiling and add it to the refreshed funding line
                if (distributionPeriod == null)
                {
                    distributionPeriod = distributionPeriodToAdd;
                    fundingLine.DistributionPeriods ??= ArraySegment<DistributionPeriod>.Empty;
                    fundingLine.DistributionPeriods = fundingLine.DistributionPeriods.Concat(new[] { distributionPeriod });
                }
            }

            return distributionPeriod;
        }

        public void SetProfilePatternKey(ProfilePatternKey profilePatternKey,
            Reference author)
        {
            SetProfilePatternKey(profilePatternKey);
            AddProfilingAudit(profilePatternKey.FundingLineCode, author);
        }

        public void SetProfilePatternKey(ProfilePatternKey profilePatternKey)
        {
            if (ProfilePatternKeys?.Any(_ => _.FundingLineCode == profilePatternKey.FundingLineCode) == true)
            {
                ProfilePatternKeys
                    .SingleOrDefault(_ => _.FundingLineCode == profilePatternKey.FundingLineCode)
                    .Key = profilePatternKey.Key;
            }
            else
            {
                ProfilePatternKeys ??= ArraySegment<ProfilePatternKey>.Empty;
                ProfilePatternKeys = ProfilePatternKeys.Concat(new[] { profilePatternKey });
            }
        }

        public void UpdateReProfileAuditETag(ReProfileAudit reProfileAudit)
        {
            if (ReProfileAudits.Any(_ => _.FundingLineCode == reProfileAudit.FundingLineCode) == true)
            {
                ReProfileAudit reProfileAuditCurrent = ReProfileAudits
                    .SingleOrDefault(_ => _.FundingLineCode == reProfileAudit.FundingLineCode);

                reProfileAuditCurrent.ETag = reProfileAudit.ETag;
            }
        }

        public void AddOrUpdateReProfileAudit(ReProfileAudit reProfileAudit)
        {
            if (ReProfileAudits.Any(_ => _.FundingLineCode == reProfileAudit.FundingLineCode) == true)
            {
                ReProfileAudit reProfileAuditCurrent = ReProfileAudits
                    .SingleOrDefault(_ => _.FundingLineCode == reProfileAudit.FundingLineCode);

                reProfileAuditCurrent.ETag = reProfileAudit.ETag;
                reProfileAuditCurrent.StrategyConfigKey = reProfileAudit.StrategyConfigKey;
                reProfileAuditCurrent.Strategy = reProfileAudit.Strategy;
                reProfileAuditCurrent.VariationPointerIndex = reProfileAudit.VariationPointerIndex;
            }
            else
            {
                (ReProfileAudits as ConcurrentBag<ReProfileAudit>).Add(reProfileAudit);
            }
        }

        [JsonIgnore]
        [IgnoreDataMember]
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
