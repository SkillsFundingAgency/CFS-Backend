using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Results
{
	public class SpecificationSummary : Reference
	{
		[JsonProperty("period")]
		public Reference Period { get; set; }

		[JsonProperty("fundingStreams")]
		public IEnumerable<Reference> FundingStreams { get; set; }
	}
}