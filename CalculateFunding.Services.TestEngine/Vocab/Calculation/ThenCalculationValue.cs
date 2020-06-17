using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Gherkin;
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
            CalculationResult calculationResult = providerResult.CalculationResults.SingleOrDefault(x => x.Calculation.Name.Equals(CalculationName, StringComparison.InvariantCultureIgnoreCase));
            object? actualValue = calculationResult.Value;
            if (decimal.TryParse(Value, out decimal expectedValue))
            {
                bool logicResult = TestLogic(expectedValue, actualValue, Operator);
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