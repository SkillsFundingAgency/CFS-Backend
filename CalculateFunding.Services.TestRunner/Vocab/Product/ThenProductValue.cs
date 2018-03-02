using System.Collections.Generic;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.TestRunner.Vocab.Product
{
    public class ThenProductValue : GherkinStepAction
    {
        public override GherkinResult Execute(CalculationResult calculationResult, List<ProviderSourceDataset> datasets,
            TestStep step)
        {
            var thenStep = step as ThenStep;
            var actualValue = calculationResult.Value;
            if (decimal.TryParse(thenStep.Value, out var expectedValue))
            {
                var logicResult = TestLogic(expectedValue, actualValue, thenStep.Operator);
                if (logicResult)
                {
                    return new GherkinResult();
                }
                else
                {
                    return new GherkinResult($"{calculationResult.Calculation.Name}- {actualValue} is not {thenStep.Operator} {expectedValue}");
                }
            }
            return new GherkinResult($"{calculationResult.Calculation.Name}- {actualValue} is not a valid number");
        }

        public override bool IsMatch(TestStepType stepType)
        {
            return stepType == TestStepType.ThenProductValue;
        }
    }
}