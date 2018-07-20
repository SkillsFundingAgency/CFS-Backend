using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.Datasets.Schema
{
    [SearchIndex(IndexerForType = typeof(DatasetDefinition),
        CollectionName = "results",
        DatabaseName = "allocations")]
    public class DatasetDefinitionIndex
    {
        [Key]
        [IsSearchable]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSearchable, IsFilterable, IsSortable]
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [IsFilterable, IsFacetable]
        [JsonProperty("providerIdentifier")]
        public string ProviderIdentifier { get; set; }

        [JsonProperty("modelHash")]
        public string ModelHash { get; set; }

        [IsFilterable, IsSortable]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset LastUpdatedDate { get; set; }
    }
}