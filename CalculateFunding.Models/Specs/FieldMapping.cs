using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class FieldMapping
    {
        [JsonProperty("matchColumn")]
        public string MatchColumn { get; set; }
    }
}