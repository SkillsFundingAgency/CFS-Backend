using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;

namespace CalculateFunding.Models.Policy
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "templateindex")]
    public class TemplateIndex
    {
        [Key]
        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSearchable, IsSortable, IsRetrievable(true)]
        [JsonProperty("name")]
        public string Name { get; set; }

        [IsFilterable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [IsFilterable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("fundingPeriodName")]
        public string FundingPeriodName { get; set; }

        [IsFilterable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }

        [IsFilterable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("fundingStreamName")]
        public string FundingStreamName { get; set; }

        [IsSortable, IsRetrievable(true)]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset LastUpdatedDate { get; set; }

        [JsonProperty("lastUpdatedAuthorId")]
        [IsRetrievable(true)]
        public string LastUpdatedAuthorId { get; set; }

        [JsonProperty("lastUpdatedAuthorName")]
        [IsRetrievable(true)]
        public string LastUpdatedAuthorName { get; set; }

        [IsFilterable, IsFacetable, IsSortable, IsRetrievable(true)]
        [JsonProperty("status")]
        public string Status { get; set; }

        [IsRetrievable(true)]
        [JsonProperty("version")]
        public int Version { get; set; }

        [IsRetrievable(true)]
        [JsonProperty("currentMajorVersion")]
        public int CurrentMajorVersion { get; set; }

        [IsRetrievable(true)]
        [JsonProperty("currentMinorVersion")]
        public int CurrentMinorVersion { get; set; }

        [IsSortable, IsRetrievable(true)]
        [JsonProperty("publishedMajorVersion")]
        public int PublishedMajorVersion { get; set; }

        [IsSortable, IsRetrievable(true)]
        [JsonProperty("publishedMinorVersion")]
        public int PublishedMinorVersion { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("hasReleasedVersion")]
        public string HasReleasedVersion { get; set; }
    }
}
