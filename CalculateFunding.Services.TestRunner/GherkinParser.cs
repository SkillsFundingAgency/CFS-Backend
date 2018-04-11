using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.Services;
using Gherkin;
using Gherkin.Ast;
using Serilog;

namespace CalculateFunding.Services.TestRunner
{
    public class GherkinParser : IGherkinParser
    {
        const string souceDatasetExpression = @"(the)(\s)+(field)(\s)+'(.*)'(\s)+(in)(\s)+(the)(\s)+(dataset)(\s)+'(.*)'(\s)+(.*)(\s)+(.*)";
        const string providerExpression = @"(the)(\s)+(provider)(\s)+(is)(\s)+'(.*)'";
        const string assertCalcExpression = @"(the)(\s)+(result)(\s)+(for)(\s)+'(.*)'(\s)+(.*)(\s)+(.*)";
        const string assertCalcDatasetExpression = @"(the)(\s)+(result)(\s)+(for)(\s)+'(.*)'(\s)+(.*)(\s)+(the)(\s)+(field)(\s)+'(.*)'(\s)+(in)(\s)+(the)(\s)+dataset(\s)+(.*)";

        static IDictionary<StepType, string> stepExpressions = new Dictionary<StepType, string>
            {
                { StepType.Datasets, souceDatasetExpression },
                { StepType.Provider, providerExpression },
                { StepType.AssertCalcDataset, assertCalcDatasetExpression },
                { StepType.AssertCalc, assertCalcExpression },
            };

        private readonly IStepParserFactory _stepParserFactory;
        private readonly ILogger _logger;

        public GherkinParser(IStepParserFactory stepParserFactory, ILogger logger)
        {
            _stepParserFactory = stepParserFactory;
            _logger = logger;
        }

        async public Task<GherkinParseResult> Parse(string gherkin, BuildProject buildProject)
        {
            GherkinParseResult result = new GherkinParseResult();
            Parser parser = new Parser();
            try
            {
                var builder = new StringBuilder();
                builder.AppendLine("Feature: Feature Wrapper");
                builder.AppendLine("  Scenario: Scenario Wrapper");
                builder.Append(gherkin);
                using (var reader = new StringReader(builder.ToString()))
                {
                    GherkinDocument document = null;
                    try
                    {
                        document = parser.Parse(reader);
                    }
                    catch (InvalidOperationException ex)
                    {
                        string buildProjectId = buildProject.Id;
                        _logger.Error(ex, $"Gherkin parser error for build project {{buildProjectId}}: {builder.ToString()}", buildProjectId);
                        throw;
                    }

                    if (document.Feature?.Children != null)
                    {
                        foreach (var scenario in document.Feature?.Children)
                        {
                            if (!scenario.Steps.IsNullOrEmpty())
                            {
                                foreach (var step in scenario.Steps)
                                {
                                    IEnumerable<KeyValuePair<StepType, string>> expression = stepExpressions.Where(m => Regex.IsMatch(step.Text, m.Value, RegexOptions.IgnoreCase));

                                    if (expression.Any())
                                    {
                                        IStepParser stepParser = _stepParserFactory.GetStepParser(expression.First().Key);

                                        if (stepParser == null)
                                            result.AddError("The supplied gherkin could not be parsed", step.Location.Line, step.Location.Column);
                                        else
                                            await stepParser.Parse(step, expression.First().Value, result, buildProject);
                                    }
                                    else
                                    {
                                        result.AddError("The supplied gherkin could not be parsed", step.Location.Line, step.Location.Column);
                                    }

                                    var keyword = step.Keyword?.ToLowerInvariant().Trim();
                                }
                            }
                            else
                            {
                                result.AddError("The supplied gherkin could not be parsed", 0, 0);
                            }
                        }
                    }
                }
            }
            catch (CompositeParserException exception)
            {
                foreach (var error in exception.Errors)
                {
                    result.AddError(error.Message, error.Location.Line, error.Location.Column);
                }
            }

            return result;
        }
    }
}
