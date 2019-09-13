using System;
using System.Collections.Generic;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    public class PublishedFundingVersion : VersionedItem
    {
        /// <summary>
        /// Cosmos Document ID
        /// </summary>
        [JsonProperty("id")]
        public override string Id => $"funding_{OrganisationGroupTypeIdentifier}_{OrganisationGroupIdentifierValue}_{FundingPeriod.Id}_{FundingStreamId}_{Version}";

        /// <summary>
        /// Funding Id
        /// eg for schema 1.0 {OrganisationGroupTypeIdentifier}_{OrganisationGroupIdentifierValue}_{FundingPeriod.Id}_{FundingStreamId}_{Version}
        /// </summary>
        [JsonProperty("fundingId")]
        public string FundingId { get; set; }

        /// <summary>
        /// Funding Stream ID
        /// </summary>
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        /// <summary>
        /// Funding Stream Name
        /// </summary>
        [JsonProperty("fundingStreamName")]
        public string FundingStreamName { get; set; }

        /// <summary>
        /// Funding Period
        /// </summary>
        [JsonProperty("fundingPeriod")]
        public PublishedFundingPeriod FundingPeriod { get; set; }

        /// <summary>
        /// Associated Specification ID
        /// </summary>
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("organisationGroupTypeCode")]
        public string OrganisationGroupTypeCode { get; set; }

        [JsonProperty("organisationGroupTypeIdentifier")]
        public string OrganisationGroupTypeIdentifier { get; set; }

        [JsonProperty("organisationGroupIdentifierValue")]
        public string OrganisationGroupIdentifierValue { get; set; }

        [JsonProperty("organisationGroupTypeCategory")]
        public string OrganisationGroupTypeCategory { get; set; }

        [JsonProperty("organisationGroupName")]
        public string OrganisationGroupName { get; set; }

        [JsonProperty("organisationGroupSearchableName")]
        public string OrganisationGroupSearchableName { get; set; }

        /// <summary>
        /// List of all additional identifiers for this organisation group
        /// </summary>
        [JsonProperty("organisationGroupIdentifiers")]
        public IEnumerable<PublishedOrganisationGroupTypeIdentifier> OrganisationGroupIdentifiers { get; set; }

        /// <summary>
        /// Funding Lines - used to store the profiling result and total for all funding lines.
        /// The total funding per funding line and distribution periods are stored here.
        /// The results are the aggregation for all provider versions contained in the Organisation Group
        /// </summary>
        [JsonProperty("fundingLines")]
        public IEnumerable<FundingLine> FundingLines { get; set; }

        /// <summary>
        /// Calculations that make up this funding line. 
        /// Should only contain calculations which have been aggregated
        /// </summary>
        [JsonProperty("calculations")]
        public IEnumerable<FundingCalculation> Calculations { get; set; }

        /// <summary>
        /// Reference data that make up data for calculations.
        /// Should only contain aggregated reference data
        /// </summary>
        [JsonProperty("referenceData")]
        public IEnumerable<FundingReferenceData> ReferenceData { get; set; }

        /// <summary>
        /// The provider funding version ID for all providers under this Organisation Group of funding.
        /// The ID refers to PublishedProviderVersion.FundingId when creating this PublishedFundingVersion
        /// </summary>
        [JsonProperty("providerFundings")]
        public IEnumerable<string> ProviderFundings { get; set; }

        /// <summary>
        /// Grouping Reason for this Organisation Group
        /// </summary>
        [JsonProperty("groupingReason")]
        public GroupingReason GroupingReason { get; set; }

        /// <summary>
        /// Status changed date - last updated date
        /// </summary>
        [JsonProperty("statusChangedDate")]
        public DateTime StatusChangedDate { get; set; }

        /// <summary>
        /// External Publication Date
        /// </summary>
        [JsonProperty("externalPublicationDate")]
        public DateTime ExternalPublicationDate { get; set; }

        /// <summary>
        /// Earliest Payment Available Date
        /// </summary>
        [JsonProperty("earliestPaymentAvailableDate")]
        public DateTime EarliestPaymentAvailableDate { get; set; }

        /// <summary>
        /// Total funding for this organisation group in pence
        /// </summary>
        [JsonProperty("totalFunding")]
        public int TotalFunding { get; set; }

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
        /// Template Version used to generate this organisation group
        /// </summary>
        [JsonProperty("templateVersion")]
        public string TemplateVersion { get; set; }

        /// <summary>
        /// Schema version used to output this funding version
        /// </summary>
        [JsonProperty("schemaVersion")]
        public string SchemaVersion { get; set; }

        /// <summary>
        /// Published Funding Status
        /// </summary>
        [JsonProperty("status")]
        public PublishedFundingStatus Status { get; set; }

        /// <summary>
        /// Cosmos entity ID for versioning - the parent PublishedFunding
        /// </summary>
        [JsonProperty("entityId")]
        public override string EntityId => $"funding_{OrganisationGroupTypeIdentifier}_{OrganisationGroupIdentifierValue}_{FundingPeriod.Id}_{FundingStreamId}";

        /// <summary>
        /// Cosmos partition Id. Should match the parent PublishFunding ID
        /// </summary>
        [JsonProperty("partitionId")]
        public string PartitionId => $"funding_{OrganisationGroupTypeIdentifier}_{OrganisationGroupIdentifierValue}_{FundingPeriod.Id}_{FundingStreamId}";

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

        public override VersionedItem Clone()
        {
            // Serialise to perform a deep copy
            string json = JsonConvert.SerializeObject(this);
            return JsonConvert.DeserializeObject<PublishedFundingVersion>(json);
        }
    }
}
