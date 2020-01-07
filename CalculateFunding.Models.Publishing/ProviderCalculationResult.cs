using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    public class ProviderCalculationResult
    {
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("results")]
        public IEnumerable<CalculationResult> Results { get; set; }
    }
}