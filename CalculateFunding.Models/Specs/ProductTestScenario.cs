using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class ProductTestScenario
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("givenSteps")]
        public List<GivenStep> GivenSteps { get; set; }
        [JsonProperty("thenSteps")]
        public List<ThenStep> ThenSteps { get; set; }
    }
}