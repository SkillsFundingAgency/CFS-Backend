using System.Collections.Generic;
using System.Linq;
using Gherkin.Ast;

namespace Allocations.Services.TestRunner
{

    public class GherkinScenarioResult : GherkinResult
    {
        public string Feature { get; set; }
        public string ScenarioName { get; set; }
        public string ScenarioDescription { get; set; }
        public int TotalSteps { get; set; }
        public int StepsExecuted { get; set; }
    }

    public class GherkinResult
    {
        public GherkinResult()
        {
            Errors = new List<GherkinError>();
        }

        public GherkinResult(string errorMessage, Location location)
        {
            Errors = new List<GherkinError>();
            AddError(errorMessage, location);
        }

        public void AddError(string errorMessage, Location location)
        {
            Errors.Add(new GherkinError(errorMessage, location));
        }

        public bool HasErrors => Errors == null || !Errors.Any();
        public List<GherkinError> Errors { get; }

        public bool Abort { get; set; }
    }
}