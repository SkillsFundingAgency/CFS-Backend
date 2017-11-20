using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Allocations.Models.Results;
using Newtonsoft.Json;

namespace Allocations.Models.Specs
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