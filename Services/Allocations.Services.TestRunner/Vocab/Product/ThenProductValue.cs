using System;
using System.Collections.Generic;
using System.Linq;
using Allocations.Models.Results;
using Allocations.Models.Specs;
using Gherkin.Ast;

namespace Allocations.Services.TestRunner.Vocab.Product
{
    public class ThenProductValue : GherkinStepAction
    {
        public ThenProductValue() : base(@"the result should be (.*) (.*)", "Then")
        {

        }

        public override GherkinResult Validate(Budget budget, Step step)
        {
            var datasetName = GetInlineArguments(step).FirstOrDefault();
            var table = step.Argument as DataTable;

            return new GherkinResult();
        }

        public override GherkinResult Execute(ProductResult productResult, List<object> datasets, Step step)
        {
            var args = GetInlineArguments(step).ToArray();
            var logic = args[0];
            var expectedValueString = args[1];

            var actualValue = productResult.Value;
            if (decimal.TryParse(expectedValueString, out var expectedValue))
            {
                var logicResult = TestLogic(expectedValue, actualValue, logic);
                if (logicResult)
                {
                    return new GherkinResult();
                }
                else
                {
                    return new GherkinResult($"{productResult.Product.Name}- {actualValue} is not {logic} {expectedValue}", step.Location);
                }
            }
            return new GherkinResult($"{productResult.Product.Name}- {actualValue} is not a valid number", step.Location);
        }
    }
}