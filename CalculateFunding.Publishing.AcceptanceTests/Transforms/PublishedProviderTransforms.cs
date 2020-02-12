using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Models;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.Transforms
{
    [Binding]
    public class PublishedProviderTransforms : TransformsBase
    {
        [StepArgumentTransformation]
        public IEnumerable<FundingLine> ToFundingLines(Table fundingLinesTable)
        {
            EnsureTableHasData(fundingLinesTable);

            return fundingLinesTable.CreateSet<FundingLine>()
                .ToArray();
        }

        [StepArgumentTransformation]
        public IEnumerable<ExpectedPredecessor> ToExpectedPredecessors(Table predecessorsTable)
        {
            EnsureTableHasData(predecessorsTable);

            return predecessorsTable.CreateSet<ExpectedPredecessor>()
                .ToArray();
        }
    }
}