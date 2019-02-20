using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace CalculateFunding.Api.External.V2.Models
{
	public class ProviderInformationModel
	{
		[JsonProperty("UkPrn")]
		public string Ukprn { get; set; }
	}
}
