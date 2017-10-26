using System.Linq;
using Allocations.Engine;
using Allocations.Models.Budgets;
using Allocations.Models.Results;
using Gherkin.Ast;

namespace Allocations.Gherkin.Vocab.Product
{
    public class ThenProductValue : GherkinStepAction
    {
        public ThenProductValue() : base(@"'(.*)' should be (.*) (.*)", "Then")
        {

        }

        public override GherkinResult Validate(Budget budget, Step step)
        {
            var datasetName = GetInlineArguments(step).FirstOrDefault();


            var table = step.Argument as DataTable;

            return new GherkinResult();
        }

        public override GherkinResult Execute(ProviderResult providerResult, Step step)
        {
            return new GherkinResult();
        }
    }
}