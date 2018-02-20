using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
	public class ProviderSummary : Reference
	{
		[JsonProperty("urn")]
		// ReSharper disable once InconsistentNaming
		public string URN { get; set; }
		[JsonProperty("ukPrn")]
		// ReSharper disable once InconsistentNaming
		public string UKPRN { get; set; }
		[JsonProperty("upin")]
		// ReSharper disable once InconsistentNaming
		public string UPIN { get; set; }
		[JsonProperty("establishmentNumber")]
		// ReSharper disable once InconsistentNaming
		public string EstablishmentNumber { get; set; }
		[JsonProperty("authority")]
		public Reference Authority { get; set; }

		[JsonProperty("providerType")]
		public Reference ProviderType { get; set; }

		[JsonProperty("providerSubType")]
		public Reference ProviderSubType { get; set; }
	}
}