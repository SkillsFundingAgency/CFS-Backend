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
}