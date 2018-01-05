using System;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class CalculationResult 
    {
        [JsonProperty("policy")]
        public Reference Policy { get; set; }
        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }
        [JsonProperty("calculation")]
        public Reference Calculation { get; set; }
        [JsonProperty("value")]
        public decimal? Value { get; set; }
        [JsonProperty("exception")]
        public Exception Exception { get; set; }
    }
}