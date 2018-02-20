using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CalculateFunding.Models.Datasets;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
	[SearchIndex(IndexerForType = typeof(Dataset),
		CollectionName = "results",
		DatabaseName = "allocations")]
	public class ProviderIndex
	{
		[Key]
		[IsSearchable]
		[JsonProperty("id")]
		public string Id { get; set; }

		[IsSearchable]
		[JsonProperty("name")]
		public string Name { get; set; }

		[IsFilterable, IsSearchable, IsFacetable]
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

	public class ProviderSummary : Reference
    {
        [JsonProperty("urn")]
        public string URN { get; set; }

        [JsonProperty("authority")]
        public Reference Authority { get; set; }

        [JsonProperty("tags")]
        public List<string> Tags { get; set; }

        [JsonProperty("phase")]
        public Reference Phase { get; set; }        
    }

    public class ProviderResult : Reference
    {
        [JsonProperty("budget")]
        public Reference Specification { get; set; }
        [JsonProperty("provider")]
        public ProviderSummary Provider { get; set; }

        [JsonProperty("calcResults")]
        public List<CalculationResult> CalculationResults { get; set; }

        [JsonProperty("sourceDatasets")]
        public List<object> SourceDatasets { get; set; }
    }
}