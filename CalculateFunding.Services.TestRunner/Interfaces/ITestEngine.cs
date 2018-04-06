using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestEngine
    {
        Task<IEnumerable<TestScenarioResult>> RunTests(IEnumerable<TestScenario> testScenarios, IEnumerable<ProviderResult> providerResults,
             IEnumerable<ProviderSourceDataset> sourceDatasets, IEnumerable<TestScenarioResult> currentResults, Specification specification, BuildProject buildProject);
    }
}
