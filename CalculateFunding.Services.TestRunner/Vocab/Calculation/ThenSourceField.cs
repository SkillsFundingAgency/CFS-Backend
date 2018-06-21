using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;

namespace CalculateFunding.Services.TestRunner.Vocab.Calculation
{
    [TestStep("then", SyntaxConstants.datasetSourceField)]
    public class ThenSourceField : GherkinStepAction
    {
        [TestStepArgument(StepArgumentType.FieldName)]
        public string FieldName { get; set; }

        [TestStepArgument(StepArgumentType.DatasetName)]
        public string DatasetName { get; set; }

        public ComparisonOperator Operator { get; set; }

        public string CalculationName { get; set; }

        public override GherkinParseResult Execute(ProviderResult providerResult, IEnumerable<ProviderSourceDatasetCurrent> datasets)
        {
            var calculationResult = providerResult.CalculationResults.SingleOrDefault(x => x.Calculation.Name == CalculationName);

            var actualValue = GetActualValue(datasets, DatasetName, FieldName);

            if (actualValue != null)
            {
                var expectedValue = Convert.ChangeType(calculationResult.Value, actualValue.GetType());
                var logicResult = TestLogic(actualValue, expectedValue, Operator);
                if (!logicResult)
                {
                    return new GherkinParseResult(
                        $"{FieldName} in {DatasetName} - {actualValue} is not {Operator} {expectedValue}")
                    {
                        Dependencies = { new Dependency(DatasetName, FieldName, actualValue?.ToString()) }                      
                    };
                }
                return new GherkinParseResult()
                {
                    Dependencies = { new Dependency(DatasetName, FieldName, actualValue?.ToString()) }
                };
            }
            return new GherkinParseResult($"{FieldName} in {DatasetName} was not found")
            {
                Dependencies = { new Dependency(DatasetName, FieldName, actualValue?.ToString()) }
            };
        }

    }
}