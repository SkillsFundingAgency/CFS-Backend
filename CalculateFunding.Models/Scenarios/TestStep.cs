using Newtonsoft.Json;

namespace CalculateFunding.Models.Scenarios
{
    public abstract class TestStep
    {
        [JsonProperty("stepType")]
        public TestStepType StepType { get; set; }
        [JsonProperty("dataset")]
        public string Dataset { get; set; }
        [JsonProperty("field")]
        public string Field { get; set; }
        [JsonProperty("operator")]
        public ComparisonOperator Operator { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }

    }
}