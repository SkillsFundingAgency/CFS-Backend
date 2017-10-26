using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Gherkin;
using Gherkin.Ast;

namespace Allocations.Gherkin
{
    public class GherkinValidator
    {
        public class GherkinError
        {
            public GherkinError(string errorMessage, Location location)
            {
                ErrorMessage = errorMessage;
                Location = location;
            }

            public string ErrorMessage { get; private set; }
            public Location Location { get; private set; }
        }

        public class GherkinResult
        {
            public GherkinResult()
            {
            }

            public GherkinResult(string errorMessage, Location location)
            {
                Errors = new[]{new GherkinError(errorMessage, location)};
            }

            public bool HasErrors => Errors == null || !Errors.Any();
            public GherkinError[] Errors { get; set; }
        }

        public class GherkinVocabDefinition
        {
            public GherkinVocabDefinition(params GherkinStepAction[] stepsAction)
            {
                StepsAction = stepsAction;
            }

            public GherkinStepAction GetAction(Step step)
            {
                foreach (var stepAction in StepsAction.Where(x => x.Keywords.Contains(step.Keyword)))
                {
                    if (stepAction.RegularExpression.IsMatch(step.Text))
                    {
                        return stepAction;
                    }
                }
                return null;
            }

            protected GherkinStepAction[] StepsAction { get; private set; }
        }

        public class ProviderDatasetStepAction : GherkinStepAction
        {
            public ProviderDatasetStepAction() : base(@"I have the following '(.*)' provider dataset:", "Given")
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
                throw new NotImplementedException();
            }
        }

        public abstract class GherkinStepAction
        {
            protected GherkinStepAction(string regularExpression, params string[] keywords)
            {
                Keywords = keywords;
                RegularExpression = new Regex(regularExpression);
            }

            public Regex RegularExpression { get; private set; }

            public string[] Keywords { get; private set; }

            protected IEnumerable<string> GetInlineArguments(Step step)
            {
                return RegularExpression.Match(step.Text).Groups.Skip(1).Select(g => g.Value);
            }

            public abstract GherkinResult Validate(Step step);

            public abstract GherkinResult Execute(Step step);
        }

        public IEnumerable<GherkinResult> Validate(TextReader textReader, GherkinVocabDefinition vocab)
        {
            var parser = new Parser();

            var doc = parser.Parse(textReader);

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
                    var action = vocab.GetAction(step);
                    if (action != null)
                    {
                        var result = action.Validate(step);
                        yield return result;
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
