using System.Collections.Generic;
using System.Text.RegularExpressions;
using Allocations.Models.Results;
using Allocations.Models.Specs;
using Gherkin.Ast;

namespace Allocations.Services.TestRunner
{
    public abstract class GherkinStepAction
    {
        protected GherkinStepAction(string regularExpression, params string[] keywords)
        {
            Keywords = keywords;
            RegularExpression = new Regex(regularExpression);
        }

        public Regex RegularExpression { get; }

        public string[] Keywords { get; }

        protected IEnumerable<string> GetInlineArguments(Step step)
        {
            var group = RegularExpression.Match(step.Text).Groups;
            for (var i = 1; i < group.Count; i++)
            {
                yield return group[i].Value;
            }
        }

        public abstract GherkinResult Validate(Budget budget, Step step);

        public abstract GherkinResult Execute(ProviderResult providerResult, Step step);
    }
}