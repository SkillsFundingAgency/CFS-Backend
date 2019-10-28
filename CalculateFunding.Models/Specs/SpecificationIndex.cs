using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "specificationindex")]
    public class SpecificationIndex
    {
        [Key]
        [IsSearchable, IsRetrievable(true)]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSearchable, IsRetrievable(true)]
        [JsonProperty("name")]
        public string Name { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("fundingPeriodName")]
        public string FundingPeriodName { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }

        [IsFilterable, IsFacetable, IsSearchable, IsRetrievable(true)]
        [JsonProperty("fundingStreamNames")]
        public string[] FundingStreamNames { get; set; }

        [IsFilterable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("fundingStreamIds")]
        public string[] FundingStreamIds { get; set; }

        [IsFilterable, IsSortable, IsRetrievable(true)]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset? LastUpdatedDate { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("dataDefinitionRelationshipIds")]
        public string[] DataDefinitionRelationshipIds { get; set; }

        [IsFilterable, IsFacetable, IsSearchable, IsRetrievable(true)]
        [JsonProperty("status")]
        public string Status { get; set; }

        [IsRetrievable(true)]
        [JsonProperty("description")]
        public string Description { get; set; }

        [IsFilterable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("isSelectedForFunding")]
        public bool IsSelectedForFunding { get; set; }
    }
}