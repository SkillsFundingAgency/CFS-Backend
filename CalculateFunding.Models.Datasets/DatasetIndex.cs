using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.Datasets
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "datasetindex")]
    public class DatasetIndex
    {
        [Key]
        [IsRetrievable(true)]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSearchable, IsRetrievable(true)]
        [JsonProperty("name")]
        public string Name { get; set; }

        [IsRetrievable(true)]
        [JsonProperty("description")]
        public string Description { get; set; }

        [IsRetrievable(true)]
        [JsonProperty("changeNote")]
        public string ChangeNote { get; set; }
        
        [IsRetrievable(true)]
        [JsonProperty("changeType")]
        public string ChangeType { get; set; }

        [IsFacetable, IsFilterable, IsRetrievable(true)]
        [JsonProperty("version")]
        public int Version { get; set; }

        [IsFilterable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("fundingPeriodNames")]
        public string[] FundingPeriodNames { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("fundingperiodIds")]
        public string[] FundingPeriodIds { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("status")]
        public string Status { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("definitionName")]
        public string DefinitionName { get; set; }

        [JsonProperty("definitionId")]
        [IsRetrievable(true)]
        public string DefinitionId { get; set; }

        [IsFilterable, IsSortable, IsRetrievable(true)]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset LastUpdatedDate { get; set; }

        [JsonProperty("lastUpdatedByName")]
        [IsRetrievable(true)]
        public string LastUpdatedByName { get; set; }

        [JsonProperty("lastUpdatedById")]
        [IsRetrievable(true)]
        public string LastUpdatedById{ get; set; }

        [JsonProperty("specificationIds")]
        [IsRetrievable(true)]
        public string[] SpecificationIds { get; set; }

        [IsFilterable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("specificationNames")]
        public string[] SpecificationNames { get; set; }

        [JsonProperty("fundingStreamId")]
        [IsFilterable, IsFacetable, IsRetrievable(true)]
        public string FundingStreamId { get; set; }

        [JsonProperty("fundingStreamName")]
        [IsFilterable, IsFacetable, IsRetrievable(true)]
        public string FundingStreamName { get; set; }
    }
}