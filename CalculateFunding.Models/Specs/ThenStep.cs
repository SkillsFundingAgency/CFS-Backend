namespace CalculateFunding.Models.Specs
{
    public class ThenStep : TestStep
    {
        public ThenStep()
        {

        }

        public ThenStep(TestStepType testType)
        {
            StepType = testType;
        }

        public ThenStep(ComparisonOperator @operator, string value)
        {
            StepType = TestStepType.ThenProductValue;
            Operator = @operator;
            Value = value;
        }

        public ThenStep(string dataset, string field, ComparisonOperator @operator, string value)
        {
            StepType = TestStepType.ThenSourceField;
            Dataset = dataset;
            Field = field;
            Operator = @operator;
            Value = value;
        }
    }
}