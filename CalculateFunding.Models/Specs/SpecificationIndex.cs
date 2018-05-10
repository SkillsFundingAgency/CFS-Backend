using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    [SearchIndex(IndexerForType = typeof(Specification),
        CollectionName = "results",
        DatabaseName = "allocations")]
    public class SpecificationIndex
    {
        [Key]
        [IsSearchable]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSearchable]
        [JsonProperty("name")]
        public string Name { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsFacetable]
        [JsonProperty("fundingPeriodName")]
        public string FundingPeriodName { get; set; }

        [IsFilterable]
        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }

        [IsFilterable, IsFacetable, IsSearchable]
        [JsonProperty("fundingStreamNames")]
        public string[] FundingStreamNames { get; set; }

        [IsFilterable, IsFacetable]
        [JsonProperty("fundingStreamIds")]
        public string[] FundingStreamIds { get; set; }

        [IsFilterable, IsSortable]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset? LastUpdatedDate { get; set; }

        [IsFilterable]
        [JsonProperty("dataDefinitionRelationshipIds")]
        public string[] DataDefinitionRelationshipIds { get; set; }
    }
}