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
        private readonly GherkinVocabDefinition _vocab;

        public GherkinExecutor(GherkinVocabDefinition vocab)
        {
            _vocab = vocab;
        }

        public IEnumerable<GherkinScenarioResult> Execute(CalculationResult calculationResult, List<object> datasets, List<TestScenario> testScenarios)
        {

            foreach (var scenario in testScenarios)
            {
                var scenarioResult = new GherkinScenarioResult { Feature = calculationResult.Calculation.Name, Scenario = new Reference(scenario.Id, scenario.Name)};

                var testSteps = new List<TestStep>();
                if (scenario.GivenSteps != null)
                {
                    testSteps.AddRange(scenario.GivenSteps);
                }
                if (scenario.ThenSteps != null)
                {
                    testSteps.AddRange(scenario.ThenSteps);
                }

                scenarioResult.TotalSteps = testSteps.Count;

                scenarioResult.StepsExecuted = 0;
                foreach (var testStep in testSteps)
                {
                    var action = _vocab.GetAction(testStep.StepType);
                    var result = action.Execute(calculationResult, datasets, testStep);
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
