using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Allocations.Models.Specs;

namespace Allocations.Functions.Results.Models
{
    public class FundingPolicy : ResultSummary
    {

        [JsonProperty("id")]
        public string Id { get; }
        [JsonProperty("name")]
        public string Name { get; }
        [JsonProperty("allocationLines")]
        public AllocationLine[] AllocationLines { get; set; }

        public FundingPolicy(string id, string name, AllocationLine[] allocationLines)
        {
            Id = id;
            Name = name;
            this.AllocationLines = allocationLines;
        }


    }
}
