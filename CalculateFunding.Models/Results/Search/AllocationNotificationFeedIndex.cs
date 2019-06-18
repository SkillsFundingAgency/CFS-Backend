using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results.Search
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "allocationnotificationfeedindex")]
    public class AllocationNotificationFeedIndex
    {
        [Key]
        [IsSearchable]
        [IsRetrievable(true)]
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        [IsRetrievable(true)]
        public string Title { get; set; }

        [JsonProperty("summary")]
        [IsRetrievable(true)]
        public string Summary { get; set; }

        [JsonProperty("datePublished")]
        [IsRetrievable(true)]
        public DateTimeOffset? DatePublished { get; set; }

        [IsSortable]
        [IsRetrievable(true)]
        [JsonProperty("dateUpdated")]
        public DateTimeOffset? DateUpdated { get; set; }

        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("fundingStreamName")]
        [IsRetrievable(true)]
        public string FundingStreamName { get; set; }

        [JsonProperty("fundingStreamShortName")]
        [IsRetrievable(true)]
        public string FundingStreamShortName { get; set; }

        [JsonProperty("fundingStreamPeriodId")]
        [IsRetrievable(true)]
        public string FundingStreamPeriodId { get; set; }

        [JsonProperty("fundingStreamStartDay")]
        [IsRetrievable(true)]
        public int FundingStreamStartDay { get; set; }

        [JsonProperty("fundingStreamStartMonth")]
        [IsRetrievable(true)]
        public int FundingStreamStartMonth { get; set; }

        [JsonProperty("fundingStreamEndDay")]
        [IsRetrievable(true)]
        public int FundingStreamEndDay { get; set; }

        [JsonProperty("fundingStreamEndMonth")]
        [IsRetrievable(true)]
        public int FundingStreamEndMonth { get; set; }

        [JsonProperty("fundingStreamPeriodName")]
        [IsRetrievable(true)]
        public string FundingStreamPeriodName { get; set; }

        [JsonProperty("fundingPeriodId")]
        [IsRetrievable(true)]
        public string FundingPeriodId { get; set; }

        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("fundingPeriodStartYear")]
        public int FundingPeriodStartYear { get; set; }

        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("fundingPeriodEndYear")]
        public int FundingPeriodEndYear { get; set; }

        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("providerUkprn")]
        public string ProviderUkPrn { get; set; }

        [JsonProperty("providerUpin")]
        [IsRetrievable(true)]
        public string ProviderUpin { get; set; }

        [JsonProperty("providerUrn")]
        [IsRetrievable(true)]
        public string ProviderUrn { get; set; }

        [JsonProperty("providerOpenDate")]
        [IsRetrievable(true)]
        public DateTimeOffset? ProviderOpenDate { get; set; }

        [JsonProperty("providerClosedDate")]
        [IsRetrievable(true)]
        public DateTimeOffset? ProviderClosedDate { get; set; }

        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("allocationLineId")]
        public string AllocationLineId { get; set; }

        [JsonProperty("allocationLineName")]
        [IsRetrievable(true)]
        public string AllocationLineName { get; set; }

        [JsonProperty("allocationLineShortName")]
        [IsRetrievable(true)]
        public string AllocationLineShortName { get; set; }

        [JsonProperty("allocationLineFundingRoute")]
        [IsRetrievable(true)]
        public string AllocationLineFundingRoute { get; set; }

        [IsFilterable]
        [JsonProperty("allocationLineContractRequired")]
        [IsRetrievable(true)]
        public bool AllocationLineContractRequired { get; set; }

        [JsonProperty("allocationVersionNumber")]
        [IsRetrievable(true)]
        public int AllocationVersionNumber { get; set; }

        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("allocationStatus")]
        public string AllocationStatus { get; set; }

        [JsonProperty("allocationAmount")]
        [IsRetrievable(true)]
        public double AllocationAmount { get; set; }

        [JsonProperty("allocationLearnerCount")]
        [IsRetrievable(true)]
        public int AllocationLearnerCount { get; set; }

        [JsonProperty("providerProfiling")]
        [IsRetrievable(true)]
        public string ProviderProfiling { get; set; }

        [JsonProperty("providerName")]
        [IsRetrievable(true)]
        public string ProviderName { get; set; }

        [JsonProperty("providerLegalName")]
        [IsRetrievable(true)]
        public string ProviderLegalName { get; set; }

        [IsFilterable]
        [JsonProperty("laCode")]
        [IsRetrievable(true)]
        public string LaCode { get; set; }

        [JsonProperty("authority")]
        [IsRetrievable(true)]
        public string Authority { get; set; }

        [IsFilterable]
        [JsonProperty("providerType")]
        [IsRetrievable(true)]
        public string ProviderType { get; set; }

        [IsFilterable]
        [JsonProperty("subProviderType")]
        [IsRetrievable(true)]
        public string SubProviderType { get; set; }

        [IsFilterable]
        [JsonProperty("establishmentNumber")]
        [IsRetrievable(true)]
        public string EstablishmentNumber { get; set; }

        [IsFilterable]
        [JsonProperty("dfeEstablishmentNumber")]
        [IsRetrievable(true)]
        public string DfeEstablishmentNumber { get; set; }

        [JsonProperty("policySummaries")]
        [IsRetrievable(true)]
        public string PolicySummaries { get; set; }

        [JsonProperty("financialEnvelopes")]
        [IsRetrievable(true)]
        public string FinancialEnvelopes { get; set; }

        [JsonProperty("calculations")]
        [IsRetrievable(true)]
        public string Calculations { get; set; }

        [JsonProperty("crmAccountId")]
        [IsRetrievable(true)]
        public string CrmAccountId { get; set; }

        [JsonProperty("navVendorNo")]
        [IsRetrievable(true)]
        public string NavVendorNo { get; set; }

        [JsonProperty("status")]
        [IsRetrievable(true)]
        public string ProviderStatus { get; set; }

        [IsFilterable]
        [JsonProperty("majorVersion")]
        [IsRetrievable(true)]
        public int? MajorVersion { get; set; }

        [IsFilterable]
        [JsonProperty("minorVersion")]
        [IsRetrievable(true)]
        public int? MinorVersion { get; set; }

        [IsFilterable]
        [JsonProperty("isDeleted")]
        [IsRetrievable(true)]
        public bool IsDeleted { get; set; }

		[JsonProperty("variationReasons")]
        [IsRetrievable(true)]
        public string[] VariationReasons { get; set; }

		[JsonProperty("successors")]
        [IsRetrievable(true)]
        public string[] Successors { get; set; }

		[JsonProperty("predecessors")]
        [IsRetrievable(true)]
        public string[] Predecessors { get; set; }

		[JsonProperty("openReason")]
        [IsRetrievable(true)]
        public string OpenReason { get; set; }

	    [JsonProperty("closeReason")]
        [IsRetrievable(true)]
        public string CloseReason { get; set; }
	}
}
