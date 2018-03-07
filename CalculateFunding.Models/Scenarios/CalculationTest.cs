using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Scenarios
{
    public class CalculationTest :  Reference
    {
        [JsonProperty("testScenarios")]
        public List<TestScenario> TestScenarios { get; set; }
    }
}