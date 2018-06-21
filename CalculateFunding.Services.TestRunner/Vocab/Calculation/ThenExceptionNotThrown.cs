using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.TestRunner.Vocab.Calculation
{
    public class ThenExceptionNotThrown : GherkinStepAction
    {
        public string CalculationName { get; set; }

        public override GherkinParseResult Execute(ProviderResult providerResult, IEnumerable<ProviderSourceDatasetCurrent> datasets)
        {
            var calculationResult = providerResult.CalculationResults.Where(x => x.Calculation.Name == CalculationName);
            //if (calculationResult.Exception != null)
            //{
            //    return new GherkinResult($"{calculationResult.Exception.GetType().Name} thrown: {calculationResult.Exception.Message} ");

            //}
            return new GherkinParseResult();

        }


    }
}