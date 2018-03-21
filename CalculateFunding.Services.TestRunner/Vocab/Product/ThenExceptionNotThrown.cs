using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;

namespace CalculateFunding.Services.TestRunner.Vocab.Product
{
    public class ThenExceptionNotThrown : GherkinStepAction
    {
        public string CalculationName { get; set; }

        public override GherkinParseResult Execute(ProviderResult providerResult, List<ProviderSourceDataset> datasets)
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