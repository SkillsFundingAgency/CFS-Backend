using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
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
        public CalculationImplementation Calculation { get; set; }
        [JsonProperty("testScenarios")]
        public List<TestScenario> TestScenarios { get; set; }
        [JsonProperty("testProviders")]
        public List<Reference> TestProviders { get; set; }
    }

}