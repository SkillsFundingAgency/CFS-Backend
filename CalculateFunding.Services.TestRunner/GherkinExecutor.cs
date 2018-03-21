using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculationResult = CalculateFunding.Models.Results.CalculationResult;

namespace CalculateFunding.Services.TestRunner
{
    public class GherkinExecutor
    {
        private readonly StepFactory _stepFactory;
        private readonly IGherkinParser _parser;

        public GherkinExecutor(StepFactory stepFactory, IGherkinParser parser)
        {
            _stepFactory = stepFactory;
            _parser = parser;
        }

        public IEnumerable<ScenarioResult> Execute(ProviderResult providerResult, List<ProviderSourceDataset> datasets, List<TestScenario> testScenarios)
        {

            foreach (var scenario in testScenarios)
            {
                var scenarioResult = new ScenarioResult
                {
                    Scenario = new Reference(scenario.Id, scenario.Name)
                };

                var parseResult = _parser.Parse(scenario.Current.Gherkin);
                
                scenarioResult.TotalSteps = parseResult.StepActions.Count;

                scenarioResult.StepsExecuted = 0;
                foreach (var action in parseResult.StepActions)
                {
                    var result = action.Execute(providerResult, datasets);
                    if (result.Dependencies.Any())
                    {
                        foreach (var resultDependency in result.Dependencies)
                        {
                            if (!scenarioResult.Dependencies.Contains(resultDependency))
                            {
                                scenarioResult.Dependencies.Add(resultDependency);
                            }
                        }
                    }
                    if (result.HasErrors)
                    {
                        scenarioResult.Errors.AddRange(result.Errors);
                    }
                    if (result.Abort)
                    {
                        break;
                    }
                    scenarioResult.StepsExecuted++;
                }

                yield return scenarioResult;
            }
        }
     }
}
