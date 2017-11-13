using Allocations.Models.Specs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Allocations.Functions.Results.Models
{
    class Budget : ResultSummary
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("academicYear")]
        public string AcademicYear { get; set; }
        [JsonProperty("fundingStream")]
        public string FundingStream { get; set; }
        [JsonProperty("fundingPolicies")]
        public FundingPolicy[] FundingPolicies { get; set; }
    }
}
