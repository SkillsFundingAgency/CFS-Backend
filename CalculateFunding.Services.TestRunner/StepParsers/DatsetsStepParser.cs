using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Code;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.Vocab.Calculation;
using Gherkin.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.StepParsers
{
    public class DatsetsStepParser : CalcStepParser, IStepParser
    {
        private readonly ICodeMetadataGeneratorService _codeMetadataGeneratorService;

        public DatsetsStepParser(ICodeMetadataGeneratorService codeMetadataGeneratorService)
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
                    parseResult.AddError("No datasets available for this test", step.Location.Line, step.Location.Column);
                }
                else
                {
                    IEnumerable<TypeInformation> typeInformation = _codeMetadataGeneratorService.GetTypeInformation(assembly);

                    string[] matches = Regex.Split(step.Text, stepExpression, RegexOptions.IgnoreCase);

                    string datasetName = matches[5];

                    string fieldName = matches[9];

                    string comparisonOperator = matches[13];

                    string value = matches[15];

                    KeyValuePair<ComparisonOperator, string> comparisonOperatorValue = ComparisonOperators.FirstOrDefault(x => x.Value == comparisonOperator);
                    if (string.IsNullOrEmpty(comparisonOperatorValue.Value))
                    {
                        parseResult.AddError("Invalid comparison operator", step.Location.Line, step.Location.Column);
                    }
                    else
                    {
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
                                parseResult.AddError($"'{fieldName}' does not exist in the dataset '{datasetName}'", step.Location.Line, step.Location.Column);
                            }
                        }

                        parseResult.StepActions.Add(new GivenSourceField
                        {
                            FieldName = fieldName,
                            DatasetName = datasetName,
                            Value = value,
                            Operator = comparisonOperatorValue.Key,
                        });
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}
