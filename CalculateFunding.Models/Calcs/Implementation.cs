using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class Implementation : Reference
    {
        [JsonProperty("specification")]
        public Reference Specification { get; set; }

        [JsonProperty("targetLanguage")]
        public TargetLanguage TargetLanguage { get; set; }
    }
}