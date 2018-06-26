using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Gherkin
{
    public class GherkinParseResult
    {
        public GherkinParseResult()
        {
            Errors = new List<GherkinError>();
            Dependencies = new List<Dependency>();
            StepActions = new List<IStepAction>();
        }

        public GherkinParseResult(string errorMessage)
        {
            Errors = new List<GherkinError>();
            Dependencies = new List<Dependency>();
            StepActions = new List<IStepAction>();
            AddError(errorMessage);
        }

        public void AddError(string message, int? line = null, int? column = null)
        {
            Errors.Add(new GherkinError(message, line, column));
        }

        public bool HasErrors => Errors != null && Errors.Any();

        public List<GherkinError> Errors { get; }

        public List<IStepAction> StepActions { get; }

        public List<Dependency> Dependencies { get; }

        public bool Abort { get; set; }
    }
}