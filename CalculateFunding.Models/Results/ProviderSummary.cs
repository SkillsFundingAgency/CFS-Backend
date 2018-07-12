using Newtonsoft.Json;
using System;

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
		public string Authority { get; set; }

		[JsonProperty("providerType")]
		public string ProviderType { get; set; }

		[JsonProperty("providerSubType")]
		public string ProviderSubType { get; set; }

        [JsonProperty("dateOpened")]
        public DateTimeOffset? DateOpened { get; set; }

        [JsonProperty("providerProfileIdType")]
        public string ProviderProfileIdType { get; set; }

        [JsonProperty("laCode")]
        public string LACode { get; set; }
    }
}