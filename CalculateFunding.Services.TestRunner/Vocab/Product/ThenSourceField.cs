using System;
using System.Collections.Generic;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.TestRunner.Vocab.Product
{

    //And 'PrimaryNOR' in 'APT Provider Information' is greater than 0
    //Then 'P004_PriRate' should be greater than 0

    public class ThenSourceField : GherkinStepAction
    {
        public override GherkinResult Execute(CalculationResult calculationResult, List<ProviderSourceDataset> datasets,
            TestStep step)
        {
            var givenStep = step as ThenStep;
            var actualValue = GetActualValue(datasets, givenStep.Dataset, givenStep.Field);

            if (actualValue != null)
            {
                var expectedValue = Convert.ChangeType(givenStep.Value, actualValue.GetType());
                var logicResult = TestLogic(expectedValue, actualValue, givenStep.Operator);
                if (!logicResult)
                {
                    return new GherkinResult(
                        $"{givenStep.Field} in {givenStep.Dataset} - {actualValue} is not {givenStep.Operator} {expectedValue}")
                    {
                        Dependencies = { new Dependency(givenStep.Dataset, givenStep.Field, actualValue?.ToString()) }                      
                    };
                }
                return new GherkinResult()
                {
                    Dependencies = { new Dependency(givenStep.Dataset, givenStep.Field, actualValue?.ToString()) }
                };
            }
            return new GherkinResult($"{givenStep.Field} in {givenStep.Dataset} was not found")
            {
                Dependencies = { new Dependency(givenStep.Dataset, givenStep.Field, actualValue?.ToString()) }
            };
        }

        public override bool IsMatch(TestStepType stepType)
        {
            return stepType == TestStepType.ThenSourceField;
        }
    }
}