using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Scenarios
{
    public class TestScenario : Reference
    {

        [JsonProperty("givenSteps")]
        public List<GivenStep> GivenSteps { get; set; }
        [JsonProperty("thenSteps")]
        public List<ThenStep> ThenSteps { get; set; }
    }
}