using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface IGherkinExecutor
    {
        Task<IEnumerable<ScenarioResult>> Execute(ProviderResult providerResult, IEnumerable<ProviderSourceDataset> datasets, IEnumerable<TestScenario> testScenarios, BuildProject buildProject);
    }
}
