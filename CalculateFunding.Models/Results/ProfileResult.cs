using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Results
{
    public class ProfileResult
    {
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("profileResults")]
        public IEnumerable<ProfileResults> ProfileResults { get; set; }
    }
}
