using System.Linq;
using Gherkin.Ast;

namespace Allocations.Services.TestRunner
{
    public abstract class GherkinVocabDefinition
    {
        protected GherkinVocabDefinition(params GherkinStepAction[] stepsAction)
        {
            StepsAction = stepsAction;
        }

        public GherkinStepAction GetAction(Step step)
        {
            foreach (var stepAction in StepsAction.Where(x => x.Keywords.Contains(step.Keyword.Trim())))
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
}