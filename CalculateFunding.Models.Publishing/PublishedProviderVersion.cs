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
        public IEnumerable<string> Predecessors { get; set; }

        /// <summary>
        /// Variation reasons
        /// </summary>
        [JsonProperty("variationReasons")]
        public IEnumerable<VariationReason> VariationReasons { get; set; }

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

        public IEnumerable<string> Variances => _variances;

        private readonly List<string> _variances;

        public PublishedProviderVersion()
        {
            _variances = new List<string>();
        }

        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<PublishedProviderVersion>(json);
        }

        public virtual bool Equals(GeneratedProviderResult providerResult, string template, Provider provider)
        {
            (bool nochange, FundingLine fundingLine) hasFundingLineChanges = CompareEnumerable(FundingLines, providerResult.FundingLines, (x, y) => {
                if (x.TemplateLineId == y.TemplateLineId && x.FundingLineCode == y.FundingLineCode && x.Name == y.Name && x.Type == y.Type && x.Value == y.Value)
                {
                    (bool nochange, DistributionPeriod distributionPeriod) hasDistributionPeriodChanges = CompareEnumerable(x.DistributionPeriods, y.DistributionPeriods, (xdp, ydp) => {
                        {
                            if (xdp.DistributionPeriodId == ydp.DistributionPeriodId && xdp.Value == ydp.Value)
                            {
                                (bool nochange, ProfilePeriod profilePeriod) hasProfilePeriodChanges = CompareEnumerable(xdp.ProfilePeriods, ydp.ProfilePeriods, (xpp, ypp) => xpp.DistributionPeriodId == ypp.DistributionPeriodId && xpp.ProfiledValue == ypp.ProfiledValue && xpp.TypeValue == ypp.TypeValue && xpp.Type == ypp.Type && xpp.Year == ypp.Year && xpp.Occurrence == ypp.Occurrence);
                                if (!hasProfilePeriodChanges.nochange)
                                {
                                    _variances.Add($"ProfilePeriod:{hasProfilePeriodChanges.profilePeriod?.DistributionPeriodId}");
                                    return false;
                                }
                            }
                            else
                            {
                                return false;
                            }

                            return true;
                        }
                    });

                    if (!hasDistributionPeriodChanges.nochange)
                    {
                        _variances.Add($"DistributionPeriod:{hasDistributionPeriodChanges.distributionPeriod?.DistributionPeriodId}");
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                return true;
            });

            if (!hasFundingLineChanges.nochange)
            {
                _variances.Add($"FundingLine:{hasFundingLineChanges.fundingLine?.FundingLineCode}");
                return false;
            }

            (bool nochange, FundingCalculation calculation) hasCalcChanges = CompareEnumerable(Calculations, providerResult.Calculations, (x, y) => x.TemplateCalculationId == y.TemplateCalculationId && x.Value.ToString() == y.Value.ToString());

            if (!hasCalcChanges.nochange)
            {
                _variances.Add($"Calculation:{hasCalcChanges.calculation?.TemplateCalculationId}");
                return false;
            }

            (bool nochange, FundingReferenceData reference) hasReferenceChanges = CompareEnumerable(ReferenceData, providerResult.ReferenceData, (x, y) => x.TemplateReferenceId == y.TemplateReferenceId && x.Value.Equals(y.Value));

            if (!hasReferenceChanges.nochange)
            {
                _variances.Add($"ReferenceData:{hasReferenceChanges.reference?.TemplateReferenceId}");
                return false;
            }

            if (TemplateVersion != template)
            {
                _variances.Add($"TemplateVersion: {TemplateVersion} != {template}");
                return false;
            }

            if (!Provider.Equals(provider))
            {
                Provider.Variances.ToList().ForEach(_ =>
                {
                    _variances.Add($"Provider: {_.Key}: {_.Value}");
                });

                return false;
            }

            return true;
        }

        private (bool, T) CompareEnumerable<T>(IEnumerable<T> firstEnumerable, IEnumerable<T> secondEnumerable, Func<T, T, bool> predicate)
        {
            T mismatchedObject = default(T);

            if (!firstEnumerable.IsNullOrEmpty())
            {
                if (!secondEnumerable.IsNullOrEmpty())
                {
                    if (firstEnumerable.Count() != secondEnumerable.Count() || !firstEnumerable.All(x => 
                    { 
                        if(secondEnumerable.Any(y => predicate(x, y)))
                        {
                            return true;
                        }

                        mismatchedObject = x;
                        return false;
                    }))
                    {
                        return (false, mismatchedObject);
                    }
                }
                else
                {
                    return (false, mismatchedObject);
                }
            }
            else
            {
                if (!secondEnumerable.IsNullOrEmpty())
                {
                    return (false, mismatchedObject);
                }
            }

            return (true, mismatchedObject);
        }
    }
}
