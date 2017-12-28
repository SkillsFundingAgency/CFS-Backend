using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class FundingPolicySummary : ResultSummary
    {

        [JsonProperty("id")]
        public string Id { get; }
        [JsonProperty("name")]
        public string Name { get; }
        [JsonProperty("allocationLines")]
        public List<AllocationLineSummary> AllocationLines { get; set; }

        public FundingPolicySummary(string id, string name, List<AllocationLineSummary> allocationLines)
        {
            Id = id;
            Name = name;
            this.AllocationLines = allocationLines;
        }


    }
}
