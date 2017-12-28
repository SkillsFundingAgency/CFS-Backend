using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class PolicySpecification : Reference
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("calculations")]
        public List<CalculationSpecification> Calculations { get; set; }

        [JsonProperty("subPolicies")]
        public List<PolicySpecification> SubPolicies { get; set; }
    }
}