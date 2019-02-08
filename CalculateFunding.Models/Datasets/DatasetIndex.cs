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
        [IsSearchable, IsFilterable]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSearchable]
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("changeNote")]
        public string ChangeNote { get; set; }

        [IsFacetable, IsFilterable]
        [JsonProperty("version")]
        public int Version { get; set; }

        [IsFilterable, IsSearchable, IsFacetable]
        [JsonProperty("fundingPeriodNames")]
        public string[] FundingPeriodNames { get; set; }

        [IsFilterable]
        [JsonProperty("fundingperiodIds")]
        public string[] FundingPeriodIds { get; set; }

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

        [JsonProperty("lastUpdatedByName")]
        public string LastUpdatedByName { get; set; }

        [JsonProperty("lastUpdatedById")]
        public string LastUpdatedById{ get; set; }

        [JsonProperty("specificationIds")]
        public string[] SpecificationIds { get; set; }

        [IsFilterable, IsFacetable, IsSearchable]
        [JsonProperty("specificationNames")]
        public string[] SpecificationNames { get; set; }

		[JsonProperty("blobName")]
		public string BlobName { get; set; }
    }
}