using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.Scenarios
{
    [SearchIndex(IndexerForType = typeof(TestScenario),
       CollectionName = "scenarios",
       DatabaseName = "allocations")]
    public class ScenarioIndex
    {
        [Key]
        [IsSearchable]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSearchable]
        [JsonProperty("name")]
        public string Name { get; set; }

        [IsSearchable]
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("specificationId")]
        [IsFilterable, IsFacetable]
        public string SpecificationId { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsFacetable]
        [JsonProperty("specificationName")]
        public string SpecificationName { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsFacetable]
        [JsonProperty("periodName")]
        public string PeriodName { get; set; }

        [IsFilterable]
        [JsonProperty("periodId")]
        public string PeriodId { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsSearchable]
        [JsonProperty("fundingStreamNames")]
        public string[] FundingStreamNames { get; set; }

        [IsFilterable, IsFacetable]
        [JsonProperty("fundingStreamIds")]
        public string[] FundingStreamIds { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsSearchable]
        [JsonProperty("status")]
        public string Status { get; set; }

        [IsFilterable, IsSortable]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset? LastUpdatedDate { get; set; }
    }
}
