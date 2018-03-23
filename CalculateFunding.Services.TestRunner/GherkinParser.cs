using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using Gherkin;

namespace CalculateFunding.Services.TestRunner
{
    [TestStep("given", "the field '(.*)' in the dataset '(.*)' (.*) (.*)")]
    public class SourceDatasetStep
    {
        [TestStepArgument(StepArgumentType.FieldName)]
        public string FieldName { get; set; }
        [TestStepArgument(StepArgumentType.DatasetName)]
        public string DatasetName { get; set; }
        public ComparisonOperator Operator { get; set; }
        public string Value { get; set; }
    }


    public class GherkinParser : IGherkinParser
    {
        private readonly Parser _parser = new Parser();

        public GherkinParseResult Parse(string gherkin)
        {
            var result = new GherkinParseResult();
            try
            {
                var builder = new StringBuilder();
                builder.AppendLine("Feature: Feature Wrapper");
                builder.AppendLine("  Scenario: Scenario Wrapper");
                builder.Append(gherkin);
                using (var reader = new StringReader(builder.ToString()))
                {
                    var document = _parser.Parse(reader);
                    if (document.Feature?.Children != null)
                    {
                        foreach (var scenario in document.Feature?.Children)
                        {
                            if (scenario.Steps != null)
                            {
                                foreach (var step in scenario.Steps)
                                {
                                    var souceDataset = "the field '(.*)' in the dataset '(.*)' (.*) (.*)";
                                    var provider = "the provider is '12345333'";
                                    var calc = "the result for '(.*)' (.*) (.*)";
                                    var calcdataset = "the result for '(.*)' (.*) (.*) the field '(.*)' in the dataset";
                                    //And 
                                    //Then the result for 'Test 123' is greater than 89
                                    //Then the result for 'Test 123' is greater than the field 'Hello' in the dataset 'Hi'""
                                    if (Regex.IsMatch(step.Text, souceDataset, RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase))
                                    {
                                        var matches = Regex.Split(step.Text, souceDataset, RegexOptions.IgnorePatternWhitespace |  RegexOptions.IgnoreCase);
                                    }

                                    var keyword = step.Keyword?.ToLowerInvariant().Trim();
                                    

                                }
                            }
                        }
                    }
                }

            }
            catch(CompositeParserException exception)
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
