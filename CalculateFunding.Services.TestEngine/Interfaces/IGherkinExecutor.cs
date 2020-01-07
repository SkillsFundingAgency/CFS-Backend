using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Scenarios;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface IGherkinExecutor
    {
        Task<IEnumerable<ScenarioResult>> Execute(ProviderResult providerResult, IEnumerable<ProviderSourceDataset> datasets, IEnumerable<TestScenario> testScenarios, BuildProject buildProject);
    }
}
