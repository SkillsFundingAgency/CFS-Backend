using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gherkin;

namespace Allocations.Gherkin
{
    public class GherkinValidator
    {
   
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
