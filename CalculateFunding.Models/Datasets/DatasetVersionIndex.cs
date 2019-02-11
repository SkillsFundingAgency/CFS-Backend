using Microsoft.Azure.Search;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.Datasets
{
	[SearchIndex(IndexerForType = typeof(Dataset),
		CollectionName = "results",
		DatabaseName = "allocations")]
	public class DatasetVersionIndex
	{
		[Key]
		[JsonProperty("id")]
		public string Id { get; set; }

		[IsFilterable]
		[JsonProperty("datasetId")]
		public string DatasetId { get; set; }
		
		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("description")]
		public string Description { get; set; }

		[JsonProperty("changeNote")]
		public string ChangeNote { get; set; }
		
		[JsonProperty("version"), IsSortable]
		public int Version { get; set; }

		[JsonProperty("definitionName")]
		public string DefinitionName { get; set; }

		[IsSortable]
		[JsonProperty("lastUpdatedDate")]
		public DateTimeOffset LastUpdatedDate { get; set; }

		[JsonProperty("lastUpdatedByName")]
		public string LastUpdatedByName { get; set; }

		[JsonProperty("blobName")]
		public string BlobName { get; set; }
	}
}