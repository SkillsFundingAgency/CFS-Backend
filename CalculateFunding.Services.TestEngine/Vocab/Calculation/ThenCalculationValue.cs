using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;

namespace CalculateFunding.Services.TestRunner.Vocab.Calculation
{
    public class ThenCalculationValue : GherkinStepAction
    {
        public string CalculationName { get; set; }
        public ComparisonOperator Operator { get; set; }
        public string Value { get; set; }

        public override GherkinParseResult Execute(ProviderResult providerResult, IEnumerable<ProviderSourceDataset> datasets)
        {
            var calculationResult = providerResult.CalculationResults.SingleOrDefault(x => x.Calculation.Name == CalculationName);
            var actualValue = calculationResult.Value;
            if (decimal.TryParse(Value, out var expectedValue))
            {
                var logicResult = TestLogic(expectedValue, actualValue, Operator);
                if (logicResult)
                {
                    return new GherkinParseResult();
                }
                else
                {
                    return new GherkinParseResult($"{calculationResult.Calculation.Name}- {actualValue} is not {Operator} {expectedValue}");
                }
            }
            return new GherkinParseResult($"{calculationResult.Calculation.Name}- {actualValue} is not a valid number");
        }
    }
}