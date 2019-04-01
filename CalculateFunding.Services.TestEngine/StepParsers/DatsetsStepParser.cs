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
        public DatsetsStepParser(ICodeMetadataGeneratorService codeMetadataGeneratorService) : base(codeMetadataGeneratorService)
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
                    parseResult.AddError("No datasets available for this test", step.Location.Line, step.Location.Column);
                }
                else
                {
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
                                parseResult.AddError($"'{fieldName}' does not exist in the dataset '{datasetName}'", step.Location.Line, step.Location.Column);
                            }
                            else
                            {
                                try
                                {
                                    Type destinationType = null;
                                    switch (fieldInfo.Type)
                                    {
                                        case "String":
                                            destinationType = typeof(string);
                                            break;
                                        case "Decimal":
                                            destinationType = typeof(decimal);
                                            break;
                                        case "Integer":
                                            destinationType = typeof(int);
                                            break;
                                        default:
                                            parseResult.AddError($"Unknown input datatype of '{fieldInfo.Type}' for field '{fieldName}' in the dataset '{datasetName}'", step.Location.Line, step.Location.Column);
                                            break;
                                    }

                                    if (destinationType != null)
                                    {
                                        object actualValue = Convert.ChangeType(value, destinationType);
                                    }

                                }
                                catch (FormatException)
                                {
                                    parseResult.AddError($"Data type mismatch for '{fieldName}' in the dataset '{datasetName}'", step.Location.Line, step.Location.Column);
                                }
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
