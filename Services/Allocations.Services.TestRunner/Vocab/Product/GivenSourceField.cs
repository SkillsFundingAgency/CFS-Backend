using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Allocations.Models;
using Allocations.Models.Results;
using Allocations.Models.Specs;
using Gherkin.Ast;

namespace Allocations.Services.TestRunner.Vocab.Product
{

    //And 'PrimaryNOR' in 'APT Provider Information' is greater than 0
    //Then 'P004_PriRate' should be greater than 0

    public class GivenSourceField : GherkinStepAction
    {
        public GivenSourceField() : base(@"'(.*)' in '(.*)' is (.*) '(.*)'", "Given")
        {

        }

        public override GherkinResult Validate(Budget budget, Step step)
        {
            var result = new GherkinResult();
            var args = GetInlineArguments(step).ToArray();
            var  fieldName =args[0];
            var datasetName = args[1];
            var logic = args[2];
            var value = args[3];
            if (string.IsNullOrEmpty(datasetName)) result.AddError("Dataset name is missing", step.Location);
            if (string.IsNullOrEmpty(fieldName)) result.AddError("Field name is missing", step.Location);
            if (!budget.DatasetDefinitions.SelectMany(x => x.FieldDefinitions).Any(x => x.Name.Equals(fieldName) || x.LongName == fieldName)) result.AddError($"{fieldName} does not exist in {datasetName}", step.Location);
            if (!budget.DatasetDefinitions.Any(x => x.Name.Equals(datasetName))) result.AddError($"{datasetName} does not exist", step.Location);

            if (string.IsNullOrEmpty(value)) result.AddError("Value is missing", step.Location);
            // step.Argument == null;

            //  var table = step.Argument as DataTable;

            return result;
        }

        public override GherkinResult Execute(ProductResult productResult, List<object> datasets, Step step)
        {

            var args = GetInlineArguments(step).ToArray();
            var fieldName = args[0];
            var datasetName = args[1];
            var logic = args[2];
            var expectedValueString = args[3];

            var actualValue = GetActualValue(datasets, datasetName, fieldName);

            if (actualValue != null)
            {
                var expectedValue = Convert.ChangeType(expectedValueString, actualValue.GetType());
                var logicResult = TestLogic(expectedValue, actualValue, logic);
                if (!logicResult)
                {
                    return new GherkinResult(
                        $"{fieldName} in {datasetName} - {actualValue} is not {logic} {expectedValue}", step.Location)
                    {
                        Abort = true,
                        Dependencies = { new Dependency(datasetName, fieldName, actualValue?.ToString()) }                      
                    };
                }
                return new GherkinResult()
                {
                    Dependencies = { new Dependency(datasetName, fieldName, actualValue?.ToString()) }
                };
            }
            return new GherkinResult($"{fieldName} in {datasetName} was not found", step.Location)
            {
                Dependencies = { new Dependency(datasetName, fieldName, actualValue?.ToString()) }
            };
        }

     }
}