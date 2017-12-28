using System;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
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
        public CalculationImplementation Product { get; set; }
        [JsonProperty("value")]
        public decimal? Value { get; set; }
        [JsonProperty("exception")]
        public Exception Exception { get; set; }
    }
}