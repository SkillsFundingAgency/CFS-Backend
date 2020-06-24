using System;
using System.ComponentModel.DataAnnotations;
using CalculateFunding.Common.Models;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "publishedfundingindex", IndexerName = "publishedfundingindexer")]
    public class PublishedFundingIndex : IIdentifiable
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
        [JsonProperty("groupTypeIdentifier")]
        public string GroupTypeIdentifier { get; set; }

        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("identifierValue")]
        public string IdentifierValue { get; set; }

        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// Document path in azure blob storage. This is populated by the indexer and is a full URL, eg https://strgt1dvprovcfs.blob.core.windows.net/publishedfunding/PES-AY-1920-Payment-LocalAuthority-12345678-1_0.json
        /// </summary>
        [IsRetrievable(true)]
        [JsonProperty("documentPath")]
        public string DocumentPath { get; set; }

        /// <summary>
        /// Is this entry deleted in blob storage
        /// </summary>
        [JsonProperty("deleted")]
        public bool? Deleted { get; set; }

        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("variationReasons")]
        public string[] VariationReasons { get; set; }
    }
}
