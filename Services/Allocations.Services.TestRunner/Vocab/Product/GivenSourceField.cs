using System.Linq;
using Allocations.Models.Results;
using Allocations.Models.Specs;
using Gherkin.Ast;

namespace Allocations.Services.TestRunner.Vocab.Product
{

    //And 'PrimaryNOR' in 'APT Provider Information' is greater than 0
    //Then 'P004_PriRate' should be greater than 0

    public class GivenSourceField : GherkinStepAction
    {
        public GivenSourceField() : base(@"'(.*)' in '(.*)' is (.*) '(.*)'", "Given")
        {

        }

        public override GherkinResult Validate(Budget budget, Step step)
        {
            var result = new GherkinResult();
            var args = GetInlineArguments(step).ToArray();
            var  fieldName =args[0];
            var datasetName = args[1];
            var logic = args[2];
            var value = args[3];
            if (string.IsNullOrEmpty(datasetName)) result.AddError("Dataset name is missing", step.Location);
            if (string.IsNullOrEmpty(fieldName)) result.AddError("Field name is missing", step.Location);
            if (!budget.DatasetDefinitions.SelectMany(x => x.FieldDefinitions).Any(x => x.Name.Equals(fieldName) || x.LongName == fieldName)) result.AddError($"{fieldName} does not exist in {datasetName}", step.Location);
            if (!budget.DatasetDefinitions.Any(x => x.Name.Equals(datasetName))) result.AddError($"{datasetName} does not exist", step.Location);

            if (string.IsNullOrEmpty(value)) result.AddError("Value is missing", step.Location);
            // step.Argument == null;

            //  var table = step.Argument as DataTable;

            return result;
        }

        public override GherkinResult Execute(ProviderResult providerResult, Step step)
        {
            var result = new GherkinResult();
            var args = GetInlineArguments(step).ToArray();
            var fieldName = args[0];
            var datasetName = args[1];
            var logic = args[2];
            var value = args[3];


            return new GherkinResult();
        }
    }
}