using System;
using System.Linq;
using Gherkin.Ast;

namespace Allocations.Gherkin.Vocab.Product
{

    //And 'PrimaryNOR' in 'APT Provider Information' is greater than 0
    //Then 'P004_PriRate' should be greater than 0

    public class GivenSourceField : GherkinStepAction
    {
        public GivenSourceField() : base(@"'(.*)' in '(.*)' is (.*) (.*)", "Given")
        {

        }

        public override GherkinResult Validate(Step step)
        {
            var datasetName = GetInlineArguments(step).FirstOrDefault();


            var table = step.Argument as DataTable;

            return new GherkinResult();
        }

        public override GherkinResult Execute(Step step)
        {
            return new GherkinResult();
        }
    }
}