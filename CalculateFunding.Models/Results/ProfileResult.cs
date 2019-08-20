using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class ProfileResult
    {
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("profileResults")]
        public IEnumerable<ProfileResults> ProfileResults { get; set; }
    }
}
