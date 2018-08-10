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

        [JsonProperty("fundingPeriodType")]
        public string FundingPeriodType { get; set; }

        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }

        [JsonProperty("fundingPeriodStartDate")]
        public DateTimeOffset FundingPeriodStartDate { get; set; }

        [IsFilterable]
        [JsonProperty("fundingPeriodStartYear")]
        public int FundingPeriodStartYear
        {
            get
            {
                return FundingPeriodStartDate.Year;
            }
        }

        [JsonProperty("fundingPeriodEndDate")]
        public DateTimeOffset FundingPeriodEndDate { get; set; }

        [IsFilterable]
        [JsonProperty("fundingPeriodEndYear")]
        public int FundingPeriodEndYear
        {
            get
            {
                return FundingPeriodEndDate.Year;
            }
        }

        [IsFilterable]
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [IsFilterable]
        [JsonProperty("providerUkprn")]
        public string ProviderUkPrn { get; set; }

        [JsonProperty("providerUpin")]
        public string ProviderUpin { get; set; }

        [JsonProperty("providerOpenDate")]
        public DateTimeOffset? ProviderOpenDate { get; set; }

        [IsFilterable]
        [JsonProperty("allocationLineId")]
        public string AllocationLineId { get; set; }

        [JsonProperty("allocationLineName")]
        public string AllocationLineName { get; set; }

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

        [JsonProperty("policySummaries")]
        public string PolicySummaries { get; set; }
    }
}
