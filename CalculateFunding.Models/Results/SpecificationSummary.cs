using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
	public class SpecificationSummary : Reference
	{
		[JsonProperty("period")]
		public Reference Period { get; set; }

		[JsonProperty("fundingStream")]
		public Reference FundingStream { get; set; }
	}
}