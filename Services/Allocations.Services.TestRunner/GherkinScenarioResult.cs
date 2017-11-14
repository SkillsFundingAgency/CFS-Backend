using Allocations.Models;

namespace Allocations.Services.TestRunner
{
    public class GherkinScenarioResult : GherkinResult
    {
        public string Feature { get; set; }
        public Reference Scenario { get; set; }
        public int TotalSteps { get; set; }
        public int StepsExecuted { get; set; }
    }
}