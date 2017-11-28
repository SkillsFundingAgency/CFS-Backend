using System.Collections.Generic;
using Allocations.Models.Results;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;

namespace Allocations.Services.TestRunner.Vocab.Product
{
    public class ThenProductValue : GherkinStepAction
    {
        public override GherkinResult Execute(ProductResult productResult, List<object> datasets, TestStep step)
        {
            var thenStep = step as ThenStep;
            var actualValue = productResult.Value;
            if (decimal.TryParse(thenStep.Value, out var expectedValue))
            {
                var logicResult = TestLogic(expectedValue, actualValue, thenStep.Operator);
                if (logicResult)
                {
                    return new GherkinResult();
                }
                else
                {
                    return new GherkinResult($"{productResult.Product.Name}- {actualValue} is not {thenStep.Operator} {expectedValue}");
                }
            }
            return new GherkinResult($"{productResult.Product.Name}- {actualValue} is not a valid number");
        }

        public override bool IsMatch(TestStepType stepType)
        {
            return stepType == TestStepType.ThenProductValue;
        }
    }
}