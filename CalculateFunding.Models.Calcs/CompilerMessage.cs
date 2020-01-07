using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
	public class CompilerMessage
    {
        [JsonProperty("severity")]
        public Severity Severity { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
	    [JsonProperty("location")]
		public SourceLocation Location { get; set; }
	}
}