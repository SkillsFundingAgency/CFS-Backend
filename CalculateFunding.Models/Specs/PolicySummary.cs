using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class PolicySummary : Reference
    {
        public PolicySummary(string id, string name, string description) : base(id, name)
        {
            Description = description;
        }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("parentPolicyId")]
        public string ParentPolicyId { get; set; }
    }
}
