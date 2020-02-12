using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Publishing;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace CalculateFunding.Publishing.AcceptanceTests.Transforms
{
    [Binding]
    public class CalculationsTransforms : TransformsBase
    {

        [StepArgumentTransformation]
        public IEnumerable<CalculationResult> ToCalculationResults(Table calculationResultsTable)
        {
            EnsureTableHasData(calculationResultsTable);

            return calculationResultsTable.CreateSet<CalculationResult>()
                       .ToArray();
        }
    }
}