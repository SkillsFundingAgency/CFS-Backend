using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Scenarios
{
    public class TestSuite : Reference
    {
        [JsonProperty("specification")]
        public Reference Specification { get; set; }
        [JsonProperty("calculationTests")]
        public List<CalculationTest> CalculationTests { get; set; }
        [JsonProperty("testProviders")]
        public List<Reference> TestProviders { get; set; }
    }

    public class CalculationTest :  Reference
    {
        [JsonProperty("testScenarios")]
         public List<TestScenario> TestScenarios { get; set; }
    }


}
