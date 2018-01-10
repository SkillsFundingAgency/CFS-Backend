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
        public List<CalculationSpecification> Calculations { get; set; }

        [JsonProperty("subPolicies")]
        public IEnumerable<Policy> SubPolicies { get; set; }
    }
}