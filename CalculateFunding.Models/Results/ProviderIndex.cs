using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Models.Results
{
    [SearchIndex()]
    public class ProviderIndex
    {

	    [Key]
	    [IsSearchable]
	    [JsonProperty("ukPrn")]
	    public string UKPRN { get; set; }

		[IsSearchable]
		[JsonProperty("urn")]
		public string URN { get; set; }

	    [IsSearchable]
	    [JsonProperty("upin")]
	    public string UPIN { get; set; }

	    [IsSearchable]
	    [JsonProperty("establishmentNumber")]
	    public string EstablishmentNumber { get; set; }

	    [IsSearchable]
	    [JsonProperty("rid")]
	    public string Rid { get; set; }

		[IsSearchable, IsSortable]
		[JsonProperty("name")]
		public string Name { get; set; }

        [IsFilterable, IsSortable, IsFacetable]
        [JsonProperty("authority")]
		public string Authority { get; set; }

	    [IsFilterable, IsSortable, IsFacetable]
	    [JsonProperty("providerType")]
	    public string ProviderType { get; set; }

	    [IsFilterable, IsSortable, IsFacetable]
	    [JsonProperty("providerSubType")]
	    public string ProviderSubType { get; set; }

	    [IsFilterable, IsSortable, IsFacetable]
	    [JsonProperty("openDate")]
		public DateTimeOffset? OpenDate { get; set; }

	    [IsFilterable, IsSortable, IsFacetable]
	    [JsonProperty("closeDate")]
		public DateTimeOffset? CloseDate { get; set; }

    }
}