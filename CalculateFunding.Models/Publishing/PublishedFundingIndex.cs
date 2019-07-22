using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "publishedfundingindex")]
    public class PublishedFundingIndex
    {
        [Key]
        [IsSearchable]
        [IsRetrievable(true)]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSortable]
        [JsonProperty("statusChangedDate")]
        [IsRetrievable(true)]
        public DateTimeOffset? StatusChangedDate { get; set; }

        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [IsFilterable]
        [JsonProperty("fundingPeriodId")]
        [IsRetrievable(true)]
        public string FundingPeriodId { get; set; }

        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("groupingType")]
        public string GroupingType { get; set; }

        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("identifierType")]
        public string IdentifierType { get; set; }

        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("identifierCode")]
        public string IdentifierCode { get; set; }

        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Document path in azure blob storage
        /// </summary>
        [IsRetrievable(true)]
        [JsonProperty("documentPath")]
        public string DocumentPath { get; set; }

        [JsonProperty("deleted")]
        public bool Deleted { get; set; }
    }
}
