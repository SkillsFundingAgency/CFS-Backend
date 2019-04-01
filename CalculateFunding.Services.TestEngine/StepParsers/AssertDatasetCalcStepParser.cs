using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Code;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.Vocab.Calculation;
using Gherkin.Ast;

namespace CalculateFunding.Services.TestRunner.StepParsers
{
    public class AssertDatasetCalcStepParser : CalcStepParser, IStepParser
    {
        public AssertDatasetCalcStepParser(ICodeMetadataGeneratorService codeMetadataGeneratorService) : base(codeMetadataGeneratorService)
        {
        }

        public Task Parse(Step step, string stepExpression, GherkinParseResult parseResult, BuildProject buildProject)
        {
            if (buildProject.Build.Assembly.IsNullOrEmpty())
            {
                parseResult.AddError("No valid assembly to test", step.Location.Line, step.Location.Column);
            }
            else
            {
                byte[] assembly = buildProject.Build.Assembly;

                if (assembly == null)
                {
                    parseResult.AddError("No calculations available for this test", step.Location.Line, step.Location.Column);
                }
                else
                {
                    string[] matches = Regex.Split(step.Text, stepExpression, RegexOptions.IgnoreCase);

                    string calcName = matches[7];

                    string fieldName = matches[19];

                    string comparison = matches[9];

                    string datasetName = matches[15];

                    MethodInformation calculation = FindCalculationMethod(assembly, calcName);

                    if (calculation == null)
                    {
                        parseResult.AddError($"Calculation: '{calcName}' was not found to test", step.Location.Line, step.Location.Column);
                    }

                    PropertyInformation dataset = FindCalculationProperty(assembly, datasetName, "Datasets");

                    if (dataset == null)
                    {
                        parseResult.AddError($"No dataset with the name '{datasetName}' could be found for this test", step.Location.Line, step.Location.Column);
                    }
                    else
                    {
                        PropertyInformation fieldInfo = FindCalculationProperty(assembly, fieldName, dataset.Type);

                        if (fieldInfo == null)
                        {
                            parseResult.AddError($"'{fieldName}' does not exis in the dataset '{datasetName}'", step.Location.Line, step.Location.Column);
                        }
                    }

                    if (!ComparisonOperators.Values.Contains(comparison.ToLower()))
                    {
                        parseResult.AddError($"'{comparison}' is not a valid comparison", step.Location.Line, step.Location.Column);
                    }

                    parseResult.StepActions.Add(new ThenSourceField
                    {
                        CalculationName = calcName,
                        Operator = ComparisonOperators.FirstOrDefault(x => x.Value == comparison).Key,
                        FieldName = fieldName,
                        DatasetName = datasetName
                    });
                }
            }

            return Task.CompletedTask;
        }
    }
}
