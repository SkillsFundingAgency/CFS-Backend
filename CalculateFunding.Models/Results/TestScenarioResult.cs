using System;
using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class TestScenarioResult : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("testResult")]
        public TestResult TestResult { get; set; }

        [JsonProperty("specification")]
        public Reference Specification { get; set; }

        [JsonProperty("testScenario")]
        public Reference TestScenario { get; set; }

        [JsonProperty("provider")]
        public Reference Provider { get; set; }
    }
}