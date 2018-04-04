﻿using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.TestRunner.Interfaces;
using System.Threading.Tasks;
using CalculationResult = CalculateFunding.Models.Results.CalculationResult;

namespace CalculateFunding.Services.TestRunner
{
    public class GherkinExecutor : IGherkinExecutor
    {
        private readonly IGherkinParser _parser;

        public GherkinExecutor(IGherkinParser parser)
        {
            _parser = parser;
        }

        public async Task<IEnumerable<ScenarioResult>> Execute(ProviderResult providerResult, IEnumerable<ProviderSourceDataset> datasets, IEnumerable<TestScenario> testScenarios, BuildProject buildProject)
        {
            IList<ScenarioResult> scenarioResults = new List<ScenarioResult>();

            foreach (var scenario in testScenarios)
            {
                var scenarioResult = new ScenarioResult
                {
                    Scenario = new Reference(scenario.Id, scenario.Name)
                };

                var parseResult = await _parser.Parse(scenario.Current.Gherkin, buildProject);
                
                if(parseResult != null && !parseResult.StepActions.IsNullOrEmpty())
                { 
                    scenarioResult.TotalSteps = parseResult.StepActions.Count;

                    scenarioResult.StepsExecuted = 0;
                    foreach (var action in parseResult.StepActions)
                    {
                        var result = action.Execute(providerResult, datasets);
                        if (!result.Dependencies.IsNullOrEmpty())
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
                }

                scenarioResults.Add(scenarioResult);
            }
            return scenarioResults;
        }
     }
}
