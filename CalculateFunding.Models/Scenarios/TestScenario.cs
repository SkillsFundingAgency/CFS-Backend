using CalculateFunding.Models.Results;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Scenarios
{
    public class TestScenario : VersionContainer<TestScenarioVersion>
    {
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }
    }
}