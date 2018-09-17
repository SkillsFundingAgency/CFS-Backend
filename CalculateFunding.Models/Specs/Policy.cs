using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class Policy : Reference
    {
        public Policy()
        {
            SubPolicies = Enumerable.Empty<Policy>();
        }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("calculations")]
        public IEnumerable<Calculation> Calculations { get; set; }

        [JsonProperty("subPolicies")]
        public IEnumerable<Policy> SubPolicies { get; set; }

        [JsonProperty("lastUpdated")]
        public DateTimeOffset LastUpdated { get; set; }
    }
}