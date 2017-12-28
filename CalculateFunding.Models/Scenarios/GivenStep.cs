namespace CalculateFunding.Models.Scenarios
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

    }
}