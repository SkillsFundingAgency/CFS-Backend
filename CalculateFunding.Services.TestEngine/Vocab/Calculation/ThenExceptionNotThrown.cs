using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Gherkin;


namespace CalculateFunding.Services.TestRunner.Vocab.Calculation
{
    public class ThenExceptionNotThrown : GherkinStepAction
    {
        public string CalculationName { get; set; }

        public override GherkinParseResult Execute(ProviderResult providerResult, IEnumerable<ProviderSourceDataset> datasets)
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