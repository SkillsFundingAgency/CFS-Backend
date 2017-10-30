using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Gherkin;
using Gherkin.Ast;

namespace Allocations.Gherkin
{
    public class GherkinExecutor
    {
        public void HasPassed(TextReader textReader)
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
                    var regex = "I have the following '(.*)' provider dataset:";
                    if (Regex.IsMatch(step.Text, regex))
                    {
                        var match = Regex.Match(step.Text, regex);
                        var argumentStrings = match.Groups.Cast<Group>().Skip(1).Select(g => g.Value);
                        var table = step.Argument as DataTable;
                        if (table?.Rows != null)
                        {
                            foreach (var row in table.Rows)
                            {

                            }
                        }
                        Console.WriteLine(step.Text);
                    }
                    Console.WriteLine(step.Text);
                }
            }

        }

    }
}