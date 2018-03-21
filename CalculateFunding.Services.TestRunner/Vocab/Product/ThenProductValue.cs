using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.TestRunner.Vocab.Product
{
    public class ThenProductValue : GherkinStepAction
    {

        public string CalculationName { get; set; }
        public ComparisonOperator Operator { get; set; }
        public string Value { get; set; }


        public override GherkinParseResult Execute(ProviderResult providerResult, List<ProviderSourceDataset> datasets)
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