using CalculateFunding.Models.Results;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Scenarios
{
    public class TestScenario : Reference
    { 
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("current")]
        public TestScenarioVersion Current { get; set; }
    }
}