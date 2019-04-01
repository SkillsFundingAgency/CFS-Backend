using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Services.TestEngine.Interfaces;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.Services;
using Gherkin;
using Gherkin.Ast;
using Serilog;

namespace CalculateFunding.Services.TestRunner
{
    public class GherkinParser : IGherkinParser
    {
        static IDictionary<StepType, string> stepExpressions = new Dictionary<StepType, string>
            {
                { StepType.AssertCalcDataset, SyntaxConstants.assertCalcDatasetExpression },
                { StepType.Datasets, SyntaxConstants.SourceDatasetStep },
                { StepType.Provider, SyntaxConstants.providerExpression },
                { StepType.AssertCalc, SyntaxConstants.assertCalcExpression },
            };

        private readonly IStepParserFactory _stepParserFactory;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly ILogger _logger;

        public GherkinParser(IStepParserFactory stepParserFactory, ICalculationsRepository calculationsRepository, ILogger logger)
        {
            Guard.ArgumentNotNull(stepParserFactory, nameof(stepParserFactory));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _stepParserFactory = stepParserFactory;
            _calculationsRepository = calculationsRepository;
            _logger = logger;
        }

        public async Task<GherkinParseResult> Parse(string specificationId, string gherkin, BuildProject buildProject)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(gherkin, nameof(gherkin));
            Guard.ArgumentNotNull(buildProject, nameof(buildProject));

            buildProject.Build.Assembly = await _calculationsRepository.GetAssemblyBySpecificationId(specificationId);

            GherkinParseResult result = new GherkinParseResult();
            Parser parser = new Parser();
            try
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendLine("Feature: Feature Wrapper");
                builder.AppendLine("  Scenario: Scenario Wrapper");
                builder.Append(gherkin);
                using (StringReader reader = new StringReader(builder.ToString()))
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
                        foreach (ScenarioDefinition scenario in document.Feature?.Children)
                        {
                            if (!scenario.Steps.IsNullOrEmpty())
                            {
                                foreach (Step step in scenario.Steps)
                                {
                                    IEnumerable<KeyValuePair<StepType, string>> expression = stepExpressions.Where(m => Regex.IsMatch(step.Text, m.Value, RegexOptions.IgnoreCase));

                                    if (expression.Any())
                                    {
                                        IStepParser stepParser = _stepParserFactory.GetStepParser(expression.First().Key);

                                        if (stepParser == null)
                                        {
                                            result.AddError("The supplied gherkin could not be parsed", step.Location.Line, step.Location.Column);
                                        }
                                        else
                                        {
                                            await stepParser.Parse(step, expression.First().Value, result, buildProject);
                                        }
                                    }
                                    else
                                    {
                                        result.AddError("The supplied gherkin could not be parsed", step.Location.Line, step.Location.Column);
                                    }

                                    string keyword = step.Keyword?.ToLowerInvariant().Trim();
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
                foreach (ParserException error in exception.Errors)
                {
                    result.AddError(error.Message, error.Location.Line, error.Location.Column);
                }
            }

            return result;
        }
    }
}
