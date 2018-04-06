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
        [JsonProperty("periodName")]
        public string PeriodName { get; set; }

        [IsFilterable]
        [JsonProperty("periodId")]
        public string PeriodId { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsSearchable]
        [JsonProperty("fundingStreamName")]
        public string FundingStreamName { get; set; }

        [IsFilterable]
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [IsFilterable, IsSortable]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset? LastUpdatedDate { get; set; }

        [IsFilterable]
        [JsonProperty("dataDefinitionRelationshipIds")]
        public string[] DataDefinitionRelationshipIds { get; set; }
    }
}