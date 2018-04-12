using System.Collections.Generic;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;

namespace CalculateFunding.Services.TestRunner.Vocab.Product
{
    public class AndProviderIs : GherkinStepAction
    {
        public string ProviderId { get; set; }
        public ComparisonOperator Operator { get; set; }
       
        public override GherkinParseResult Execute(ProviderResult providerResult, IEnumerable<ProviderSourceDataset> datasets)
        {
            var logicResult = TestLogic(ProviderId, providerResult.Provider.Id, Operator);
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