using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Allocations.Models.Results;
using Allocations.Models.Specs;
using Gherkin;
using Gherkin.Ast;

namespace Allocations.Services.TestRunner
{
    public class GherkinExecutor
    {
        private readonly GherkinVocabDefinition _vocab;
        private readonly Parser _parser;

        public GherkinExecutor(GherkinVocabDefinition vocab)
        {
            _vocab = vocab;
            _parser = new Parser();
        }

        public IEnumerable<GherkinScenarioResult> Execute(ProductResult productResult, List<object> datasets, string gherkin)
        {
            var doc = _parser.Parse(new StringReader(gherkin));

            foreach (var scenario in doc.Feature.Children.Where(x => x.Keyword == "Scenario"))
            {
                var scenarioResult = new GherkinScenarioResult { Feature = doc.Feature.Name, ScenarioName = scenario.Name, ScenarioDescription = scenario.Description };
                var steps = scenario.Steps.ToArray();
                scenarioResult.TotalSteps = steps.Length;
                scenarioResult.StepsExecuted = 0;
                foreach (var step in scenario.Steps)
                {
                    var action = _vocab.GetAction(step);
                    if (action != null)
                    {
                        var result = action.Execute(productResult, datasets, step);
                        if (result.Dependencies.Any())
                        {
                            foreach (var resultDependency in result.Dependencies)
                            {
                                if (!scenarioResult.Dependencies.Contains(resultDependency))
                                {
                                    scenarioResult.Dependencies.Add(resultDependency);
                                }
                            }
                        }
                        if (result.HasErrors)
                        {
                            scenarioResult.Errors.AddRange(result.Errors);
                        }
                        if (result.Abort)
                        {
                            break;
                        }
                        scenarioResult.StepsExecuted++;
                    }
                    else
                    {
                        scenarioResult.Errors.Add(new GherkinError("Does not match defined syntax", step.Location));
                    }
                }

                yield return scenarioResult;
            }
        }


        private IEnumerable<GherkinResult> Validate(Budget budget, GherkinDocument doc)
        {
            var background = doc.Feature.Children.FirstOrDefault(x => x.Keyword == "Background");

            foreach (var scenario in doc.Feature.Children.Where(x => x.Keyword == "Scenario"))
            {
                if (background != null)
                {
                    foreach (var step in background.Steps)
                    {
                        Console.WriteLine(step.Text);
                    }
                }

                foreach (var step in scenario.Steps)
                {
                    var action = _vocab.GetAction(step);
                    if (action != null)
                    {
                        var result = action.Validate(budget, step);
                        if (!result.Abort)
                        {
                            yield return result;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        yield return new GherkinResult("Does not match defined syntax", step.Location);
                        // not valid does not match vocab
                    }
                }
            }
        }
    }
}
