using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Code;
using CalculateFunding.Models.Gherkin;
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
    public class AssertCalcStepParser : CalcStepParser, IStepParser
    {
        private readonly ICodeMetadataGeneratorService _codeMetadataGeneratorService;

        public AssertCalcStepParser(ICodeMetadataGeneratorService codeMetadataGeneratorService)
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

                    string comparison = matches[9];

                    string value = matches[11];

                    IEnumerable<TypeInformation> typeInformation = _codeMetadataGeneratorService.GetTypeInformation(assembly);

                    MethodInformation calculation = typeInformation.FirstOrDefault(m => m.Type == "Calculations")?.Methods.FirstOrDefault(m => m.FriendlyName == calcName.Replace("'",""));

                    if (calculation == null)
                    {
                        parseResult.AddError($"Calculation: '{calcName}' was not found to test", step.Location.Line, step.Location.Column);
                    }

                    if (!ComparisonOperators.Values.Contains(comparison.ToLower()))
                    {
                        parseResult.AddError($"'{comparison}' is not a valid comparison", step.Location.Line, step.Location.Column);
                    }

                    if (!Decimal.TryParse(value, out var result))
                    {
                        parseResult.AddError($"'{value}' is not a valid decimal", step.Location.Line, step.Location.Column);
                    }

                    parseResult.StepActions.Add(new ThenCalculationValue
                    {
                        CalculationName = calcName,
                        Operator = ComparisonOperators.FirstOrDefault(x => x.Value == comparison).Key,
                        Value = value
                    });
                }

            }

            return Task.CompletedTask;
        }
    }
}
