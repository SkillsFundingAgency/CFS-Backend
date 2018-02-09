using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.Datasets
{
    [SearchIndex(IndexerForType = typeof(Dataset),
        CollectionName = "results",
        DatabaseName = "allocations")]
    public class DatasetIndex
    {
        [Key]
        [IsSearchable]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSearchable]
        [JsonProperty("name")]
        public string Name { get; set; }

        [IsFilterable, IsSortable, IsFacetable]
        [JsonProperty("periodNames")]
        public string[] PeriodNames { get; set; }

        [IsFilterable]
        [JsonProperty("periodIds")]
        public string[] PeriodIds { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsSearchable]
        [JsonProperty("status")]
        public string Status { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsFacetable]
        [JsonProperty("definitionName")]
        public string DefinitionName { get; set; }

        [JsonProperty("definitionId")]
        public string DefinitionId { get; set; }

        [IsFilterable, IsSortable]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset LastUpdatedDate { get; set; }

        [JsonProperty("specificationIds")]
        public string[] SpecificationIds { get; set; }

        [IsFilterable, IsFacetable, IsSearchable]
        [JsonProperty("specificationNames")]
        public string[] SpecificationNames { get; set; }
    }
}
