using System.Collections.Generic;
using Allocations.Models.Results;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;

namespace Allocations.Services.TestRunner.Vocab.Product
{
    public class ThenExceptionNotThrown : GherkinStepAction
    {
        public override GherkinResult Execute(ProductResult productResult, List<object> datasets, TestStep step)
        {
            if (productResult.Exception != null)
            {
                return new GherkinResult($"{productResult.Exception.GetType().Name} thrown: {productResult.Exception.Message} ");

            }
            return new GherkinResult();

        }

        public override bool IsMatch(TestStepType stepType)
        {
            return stepType == TestStepType.ThenExceptionNotThrown;
        }
    }
}