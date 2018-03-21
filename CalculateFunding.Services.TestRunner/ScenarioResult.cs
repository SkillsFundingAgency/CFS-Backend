using CalculateFunding.Models;

namespace CalculateFunding.Services.TestRunner
{
    public class ScenarioResult : GherkinParseResult
    {
        public string Feature { get; set; }
        public Reference Scenario { get; set; }
        public int TotalSteps { get; set; }
        public int StepsExecuted { get; set; }
    }
}