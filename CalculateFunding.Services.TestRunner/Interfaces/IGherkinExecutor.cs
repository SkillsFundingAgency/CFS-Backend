using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface IGherkinExecutor
    {
        IEnumerable<ScenarioResult> Execute(ProviderResult providerResult, IEnumerable<ProviderSourceDataset> datasets, IEnumerable<TestScenario> testScenarios, BuildProject buildProject);
    }
}
