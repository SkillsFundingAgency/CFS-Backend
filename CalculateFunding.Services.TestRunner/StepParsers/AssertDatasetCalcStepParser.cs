using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Code;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.Vocab.Product;
using Gherkin.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.StepParsers
{
    public class AssertDatasetCalcStepParser : CalcStepParser, IStepParser
    {
        private readonly ICodeMetadataGeneratorService _codeMetadataGeneratorService;

        public AssertDatasetCalcStepParser(ICodeMetadataGeneratorService codeMetadataGeneratorService)
        {
            _codeMetadataGeneratorService = codeMetadataGeneratorService;
        }

        public Task Parse(Step step, string stepExpression, GherkinParseResult parseResult, BuildProject buildProject)
        {
            if (string.IsNullOrWhiteSpace(buildProject.Build.AssemblyBase64))
            {
                parseResult.AddError("No valid assembly to test", step.Location.Line, step.Location.Column);
            }
            else
            {
                byte[] assembly = Convert.FromBase64String(buildProject.Build.AssemblyBase64);

                if (assembly == null)
                {
                    parseResult.AddError("No calculations available for this test", step.Location.Line, step.Location.Column);
                }
                else
                {
                    string[] matches = Regex.Split(step.Text, stepExpression, RegexOptions.IgnoreCase);

                    string calcName = matches[7];

                    string fieldName = matches[15];

                    string comparison = matches[9];

                    string datasetName = matches[22];

                    IEnumerable<TypeInformation> typeInformation = _codeMetadataGeneratorService.GetTypeInformation(assembly);

                    MethodInformation calculation = typeInformation.FirstOrDefault(m => m.Type == "Calculations")?.Methods.FirstOrDefault(m => m.FriendlyName == calcName.Replace("'", ""));

                    if (calculation == null)
                    {
                        parseResult.AddError($"Calculation: '{calcName}' was not found to test", step.Location.Line, step.Location.Column);
                    }

                    PropertyInformation dataset = typeInformation.FirstOrDefault(m => m.Type == "Datasets")?.Properties.FirstOrDefault(m => m.FriendlyName == datasetName.Replace("'", ""));

                    if (dataset == null)
                    {
                        parseResult.AddError($"No dataset with the name '{datasetName}' could be found for this test", step.Location.Line, step.Location.Column);
                    }
                    else
                    {
                        string type = dataset.Type;

                        PropertyInformation fieldInfo = typeInformation.FirstOrDefault(m => m.Type == type)?.Properties.FirstOrDefault(m => m.FriendlyName == fieldName.Replace("'", ""));

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
