using System.Collections.Generic;
using CalculateFunding.Models.Results;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{

    public class Product : ResultSummary
    {
        [JsonProperty("id")]
        public string Id => $"{Name}".ToSlug();
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("calculation")]
        public ProductCalculation Calculation { get; set; }
        [JsonProperty("testScenarios")]
        public List<ProductTestScenario> TestScenarios { get; set; }
        [JsonProperty("testProviders")]
        public List<Reference> TestProviders { get; set; }
    }

}