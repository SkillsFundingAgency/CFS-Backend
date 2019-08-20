using System;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Obsoleted
{
    [Obsolete]
    public class ProviderLookup
    {
        [JsonProperty("providerType")]
        public string ProviderType { get; set; }

        [JsonProperty("providerSubType")]
        public string ProviderSubType { get; set; }
    }
}
