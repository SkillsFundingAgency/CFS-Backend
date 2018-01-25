using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
	public class SourceLocation
	{
		[JsonProperty("owner")]
		public Reference Owner { get; set; }
		[JsonProperty("startLine")]
		public int StartLine { get; set; }
		[JsonProperty("startChar")]
		public int StartChar { get; set; }
		[JsonProperty("endLine")]
		public int EndLine { get; set; }
		[JsonProperty("endChar")]
		public int EndChar { get; set; }
	}
}