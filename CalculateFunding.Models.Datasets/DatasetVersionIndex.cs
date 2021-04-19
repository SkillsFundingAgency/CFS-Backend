using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "datasetversionindex")]
    public class DatasetVersionIndex
    {
        [Key]
        [JsonProperty("id")]
        [IsRetrievable(true)]
        public string Id { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("datasetId")]
        public string DatasetId { get; set; }

        [JsonProperty("name")]
        [IsSearchable, IsSortable, IsRetrievable(true)]
        public string Name { get; set; }

        [JsonProperty("description")]
        [IsRetrievable(true)]
        public string Description { get; set; }

        [JsonProperty("changeNote")]
        [IsRetrievable(true)]
        public string ChangeNote { get; set; }

        [JsonProperty("changeType")]
        [IsRetrievable(true)]
        public string ChangeType { get; set; }

        [JsonProperty("version")]
        [IsSortable, IsRetrievable(true)]
        public int Version { get; set; }

        [JsonProperty("definitionName")]
        [IsFilterable, IsSortable, IsRetrievable(true)]
        public string DefinitionName { get; set; }

        [IsSortable, IsRetrievable(true)]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset LastUpdatedDate { get; set; }

        [JsonProperty("lastUpdatedByName")]
        [IsRetrievable(true)]
        public string LastUpdatedByName { get; set; }

        [JsonProperty("blobName")]
        [IsRetrievable(true)]
        public string BlobName { get; set; }

        [JsonProperty("fundingStreamId")]
        [IsFilterable, IsFacetable, IsRetrievable(true)]
        public string FundingStreamId { get; set; }

        [JsonProperty("fundingStreamName")]
        [IsFilterable, IsFacetable, IsRetrievable(true)]
        public string FundingStreamName { get; set; }
    }
}