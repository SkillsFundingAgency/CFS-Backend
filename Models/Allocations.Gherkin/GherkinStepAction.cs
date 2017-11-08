using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Gherkin.Ast;

namespace Allocations.Gherkin
{
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
}