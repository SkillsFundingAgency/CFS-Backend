using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.Scenarios
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "scenarioindex")]
    public class ScenarioIndex
    {
        [Key]
        [IsSearchable, IsRetrievable(true)]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSearchable, IsRetrievable(true)]
        [JsonProperty("name")]
        public string Name { get; set; }

        [IsSearchable, IsRetrievable(true)]
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("specificationId")]
        [IsFilterable, IsFacetable, IsRetrievable(true)]
        public string SpecificationId { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("specificationName")]
        public string SpecificationName { get; set; }

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

        [IsFilterable, IsSortable, IsFacetable, IsSearchable, IsRetrievable(true)]
        [JsonProperty("status")]
        public string Status { get; set; }

        [IsFilterable, IsSortable, IsRetrievable(true)]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset? LastUpdatedDate { get; set; }
    }
}
