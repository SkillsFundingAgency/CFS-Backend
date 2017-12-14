using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class ProductSummary : ResultSummary
    {
        public ProductSummary(string id, string name, CalculationImplementation calculation, string description, List<ProductTestScenario> testScenarios)
        {
            Name = name;
            this.Calculation = calculation;
            Description = description;
            this.TestScenarios = testScenarios;
        }

        [JsonProperty("id")]
        public string Id => $"{Name}".ToSlug();
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("calculation")]
        public CalculationImplementation Calculation { get; set; }
        [JsonProperty("testScenarios")]
        public List<ProductTestScenario> TestScenarios { get; set; }
        [JsonProperty("testProviders")]
        public List<Reference> TestProviders { get; set; }
    }
}