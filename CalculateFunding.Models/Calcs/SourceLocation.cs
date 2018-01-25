using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
	public class SourceLocation
	{
		[JsonProperty("mappedId")]
		public string MappedId { get; set; }
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