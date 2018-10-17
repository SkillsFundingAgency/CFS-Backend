using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CalculateFunding.Models.Results
{
    [SearchIndex()]
    public class AllocationNotificationFeedIndex
    {
        [Key]
        [IsSearchable]
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("datePublished")]
        public DateTimeOffset? DatePublished { get; set; }

        [IsSortable]
        [JsonProperty("dateUpdated")]
        public DateTimeOffset? DateUpdated { get; set; }

        [IsFilterable]
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("fundingStreamName")]
        public string FundingStreamName { get; set; }

        [JsonProperty("fundingStreamShortName")]
        public string FundingStreamShortName { get; set; }

        [JsonProperty("fundingStreamPeriodId")]
        public string FundingStreamPeriodId { get; set; }

        [JsonProperty("fundingStreamStartDay")]
        public int FundingStreamStartDay { get; set; }

        [JsonProperty("fundingStreamStartMonth")]
        public int FundingStreamStartMonth { get; set; }

        [JsonProperty("fundingStreamEndDay")]
        public int FundingStreamEndDay { get; set; }

        [JsonProperty("fundingStreamEndMonth")]
        public int FundingStreamEndMonth { get; set; }

        [JsonProperty("fundingStreamPeriodName")]
        public string FundingStreamPeriodName { get; set; }

        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }

        [IsFilterable]
        [JsonProperty("fundingPeriodStartYear")]
        public int FundingPeriodStartYear { get; set; }

        [IsFilterable]
        [JsonProperty("fundingPeriodEndYear")]
        public int FundingPeriodEndYear { get; set; }

        [IsFilterable]
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [IsFilterable]
        [JsonProperty("providerUkprn")]
        public string ProviderUkPrn { get; set; }

        [JsonProperty("providerUpin")]
        public string ProviderUpin { get; set; }

        [JsonProperty("providerUrn")]
        public string ProviderUrn { get; set; }

        [JsonProperty("providerOpenDate")]
        public DateTimeOffset? ProviderOpenDate { get; set; }

        [JsonProperty("providerClosedDate")]
        public DateTimeOffset? ProviderClosedDate { get; set; }

        [IsFilterable]
        [JsonProperty("allocationLineId")]
        public string AllocationLineId { get; set; }

        [JsonProperty("allocationLineName")]
        public string AllocationLineName { get; set; }

        [JsonProperty("allocationLineShortName")]
        public string AllocationLineShortName { get; set; }

        [JsonProperty("allocationLineFundingRoute")]
        public string AllocationLineFundingRoute { get; set; }

        [JsonProperty("allocationLineContractRequired")]
        public bool AllocationLineContractRequired { get; set; }

        [JsonProperty("allocationVersionNumber")]
        public int AllocationVersionNumber { get; set; }

        [IsFilterable]
        [JsonProperty("allocationStatus")]
        public string AllocationStatus { get; set; }

        [JsonProperty("allocationAmount")]
        public double AllocationAmount { get; set; }

        [JsonProperty("allocationLearnerCount")]
        public int AllocationLearnerCount { get; set; }

        [JsonProperty("providerProfiling")]
        public string ProviderProfiling { get; set; }

        [JsonProperty("providerName")]
        public string ProviderName { get; set; }

        [JsonProperty("providerLegalName")]
        public string ProviderLegalName { get; set; }

        [IsFilterable]
        [JsonProperty("laCode")]
        public string LaCode { get; set; }

        [JsonProperty("authority")]
        public string Authority { get; set; }

        [IsFilterable]
        [JsonProperty("providerType")]
        public string ProviderType { get; set; }

        [IsFilterable]
        [JsonProperty("subProviderType")]
        public string SubProviderType { get; set; }

        [IsFilterable]
        [JsonProperty("establishmentNumber")]
        public string EstablishmentNumber { get; set; }

        [IsFilterable]
        [JsonProperty("dfeEstablishmentNumber")]
        public string DfeEstablishmentNumber { get; set; }

        [JsonProperty("policySummaries")]
        public string PolicySummaries { get; set; }

        [JsonProperty("crmAccountId")]
        public string CrmAccountId { get; set; }

        [JsonProperty("navVendorNo")]
        public string NavVendorNo { get; set; }

        [JsonProperty("status")]
        public string ProviderStatus { get; set; }

        [IsFilterable]
        [JsonProperty("majorVersion")]
        public int? MajorVersion { get; set; }

        [IsFilterable]
        [JsonProperty("minorVersion")]
        public int? MinorVersion { get; set; }
    }
}
