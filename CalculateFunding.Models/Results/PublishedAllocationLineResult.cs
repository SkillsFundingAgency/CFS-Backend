using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;
using System;
using System.Linq;

namespace CalculateFunding.Models.Results
{
    public class PublishedAllocationLineResult
    {
        [JsonProperty("allocationLine")]
        public AllocationLine AllocationLine { get; set; }

        [JsonProperty("current")]
        public PublishedAllocationLineResultVersion Current { get; set; }
    }
}
