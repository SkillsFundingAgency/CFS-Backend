using Newtonsoft.Json;

namespace Allocations.Models.Specs
{
    public abstract class TestStep
    {
        [JsonProperty("stepType")]
        public TestStepType StepType { get; set; }
    }
}