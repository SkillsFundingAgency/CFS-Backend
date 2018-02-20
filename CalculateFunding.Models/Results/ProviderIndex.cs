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
		[JsonProperty("urn")]
        // ReSharper disable once InconsistentNaming
		public string URN { get; set; }

        [IsSearchable]
		[JsonProperty("ukPrn")]
        // ReSharper disable once InconsistentNaming
		public string UKPRN { get; set; }

	    [IsSearchable]
	    [JsonProperty("upin")]
	    // ReSharper disable once InconsistentNaming
	    public string UPIN { get; set; }

	    [IsSearchable]
	    [JsonProperty("establishmentNumber")]
	    // ReSharper disable once InconsistentNaming
	    public string EstablishmentNumber { get; set; }

	    [IsSearchable]
	    [JsonProperty("rid")]
	    // ReSharper disable once InconsistentNaming
	    public string Rid { get; set; }

		[IsSearchable]
	    [JsonProperty("name")]
		public string Name { get; set; }

        [IsFacetable]
        [JsonProperty("authority")]
		public string Authority { get; set; }

	    [IsFacetable]
	    [JsonProperty("providerType")]
	    public string ProviderType { get; set; }

	    [IsFacetable]
	    [JsonProperty("providerSubType")]
	    public string ProviderSubType { get; set; }

	    [IsFacetable]
		public DateTimeOffset? OpenDate { get; set; }

	    [IsFacetable]
		public DateTimeOffset? CloseDate { get; set; }

    }
}