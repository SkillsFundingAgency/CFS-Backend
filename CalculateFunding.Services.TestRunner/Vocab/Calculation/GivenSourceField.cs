using System;
using System.Collections.Generic;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;

namespace CalculateFunding.Services.TestRunner.Vocab.Calculation
{
    [TestStep("given", SyntaxConstants.sourceFieldThen)]
    public class GivenSourceField : GherkinStepAction
    {
        [TestStepArgument(StepArgumentType.FieldName)]
        public string FieldName { get; set; }

        [TestStepArgument(StepArgumentType.DatasetName)]
        public string DatasetName { get; set; }

        public ComparisonOperator Operator { get; set; }

        public string Value { get; set; }

        public override GherkinParseResult Execute(ProviderResult providerResult, IEnumerable<ProviderSourceDataset> datasets)
        {
            var actualValue = GetActualValue(datasets, DatasetName, FieldName);

            if (actualValue != null)
            {
                var expectedValue = Convert.ChangeType(Value, actualValue.GetType());
                var logicResult = TestLogic(expectedValue, actualValue, Operator);
                if (!logicResult)
                {
                    return new GherkinParseResult(
                        $"{FieldName} in {DatasetName} - {actualValue} is not {Operator} {expectedValue}")
                    {
                        Abort = true,
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