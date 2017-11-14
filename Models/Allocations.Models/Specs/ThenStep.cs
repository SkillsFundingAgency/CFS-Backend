using Newtonsoft.Json;

namespace Allocations.Models.Specs
{
    public class ThenStep : TestStep
    {
        public ThenStep()
        {

        }
        public ThenStep(ComparisonOperator @operator, string value)
        {
            StepType = TestStepType.ThenProductValue;
            Operator = @operator;
            Value = value;
        }

        [JsonProperty("operator")]
        public ComparisonOperator Operator { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}