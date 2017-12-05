using System.Collections.Generic;
using Allocations.Models.Results;
using Newtonsoft.Json;

namespace Allocations.Models.Specs
{
    public class ProductTestScenario : ResultSummary
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