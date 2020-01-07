using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Scenarios;

namespace CalculateFunding.Services.TestRunner.Vocab.Calculation
{
    public class AndProviderIs : GherkinStepAction
    {
        public string ProviderId { get; set; }
        public ComparisonOperator Operator { get; set; }
       
        public override GherkinParseResult Execute(ProviderResult providerResult, IEnumerable<ProviderSourceDataset> datasets)
        {
            bool logicResult = TestLogic(ProviderId, providerResult.Provider.Id, Operator);
            if (logicResult)
            {
                return new GherkinParseResult();
            }
            else
            {
                return new GherkinParseResult { Abort = true };
            }
        }
    }
}