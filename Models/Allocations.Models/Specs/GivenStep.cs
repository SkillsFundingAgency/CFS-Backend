using Newtonsoft.Json;

namespace Allocations.Models.Specs
{
    public class GivenStep: TestStep
    {
        public GivenStep()
        {
            
        }
        public GivenStep(string dataset, string field, ComparisonOperator @operator, string value)
        {
            StepType = TestStepType.GivenSourceField;
            Dataset = dataset;
            Field = field;
            Operator = @operator;
            Value = value;
        }
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