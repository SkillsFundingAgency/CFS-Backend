using Newtonsoft.Json;

namespace CalculateFunding.Models.Policy
{
    public class ProviderLookup
    {
        [JsonProperty("providerType")]
        public string ProviderType { get; set; }

        [JsonProperty("providerSubType")]
        public string ProviderSubType { get; set; }
    }
}
