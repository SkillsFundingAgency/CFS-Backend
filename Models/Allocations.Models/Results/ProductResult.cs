using System;
using System.Runtime.Serialization;
using Allocations.Models.Specs;
using Newtonsoft.Json;
using Newtonsoft.Json.Schema;

namespace Allocations.Models.Results
{
    public class ProductResult 
    {

        [JsonProperty("fundingPolicy")]
        public Reference FundingPolicy { get; set; }
        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }
        [JsonProperty("productFolder")]
        public Reference ProductFolder { get; set; }
        [JsonProperty("product")]
        public Product Product { get; set; }
        [JsonProperty("value")]
        public decimal? Value { get; set; }
        [JsonProperty("exception")]
        public Exception Exception { get; set; }
    }
}