using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.Datasets.Schema
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "datasetdefinitionindex")]
    public class DatasetDefinitionIndex
    {
        [Key]
        [IsSearchable, IsRetrievable(true)]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSearchable, IsFilterable, IsSortable, IsRetrievable(true)]
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        [IsRetrievable(true)]
        public string Description { get; set; }

        [JsonProperty("fundingStreamId")]
        [IsFilterable, IsFacetable, IsRetrievable(true)]
        public string FundingStreamId { get; set; }

        [JsonProperty("fundingStreamName")]
        [IsFilterable, IsFacetable, IsRetrievable(true)]
        public string FundingStreamName { get; set; }

        [IsFilterable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("providerIdentifier")]
        public string ProviderIdentifier { get; set; }

        [JsonProperty("modelHash")]
        [IsRetrievable(true)]
        public string ModelHash { get; set; }

        [IsFilterable, IsSortable, IsRetrievable(true)]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset LastUpdatedDate { get; set; }
    }
}