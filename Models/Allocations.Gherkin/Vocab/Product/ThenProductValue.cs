using System.Linq;
using Gherkin.Ast;
using Allocations.Models.Budgets;
using Allocations.Models.Results;

namespace Allocations.Gherkin.Vocab.Product
{
    public class ThenProductValue : GherkinStepAction
    {
        public ThenProductValue() : base(@"'(.*)' should be (.*) (.*)", "Then")
        {

        }

        public override GherkinResult Validate(Budget budget, Step step)
        {
            var args = GetInlineArguments(step).ToArray();
            var fieldName = args[0];
            var datasetName = args[1];
            var logic = args[2];
            var value = args[3];

            // step.Argument == null;

            //  var table = step.Argument as DataTable;

            return new GherkinResult();
        }

        public override GherkinResult Execute(ProviderResult providerResult, Step step)
        {
            return new GherkinResult();
        }
    }
}