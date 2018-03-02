using System.Collections.Generic;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.TestRunner.Vocab.Product
{
    public class ThenExceptionNotThrown : GherkinStepAction
    {
        public override GherkinResult Execute(CalculationResult calculationResult, List<ProviderSourceDataset> datasets,
            TestStep step)
        {
            if (calculationResult.Exception != null)
            {
                return new GherkinResult($"{calculationResult.Exception.GetType().Name} thrown: {calculationResult.Exception.Message} ");

            }
            return new GherkinResult();

        }

        public override bool IsMatch(TestStepType stepType)
        {
            return stepType == TestStepType.ThenExceptionNotThrown;
        }
    }
}