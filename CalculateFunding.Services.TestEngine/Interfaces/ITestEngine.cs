using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Scenarios;
using System.Collections.Generic;
using System.Threading.Tasks;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestEngine
    {
        Task<IEnumerable<TestScenarioResult>> RunTests(IEnumerable<TestScenario> testScenarios, IEnumerable<ProviderResult> providerResults,
             IEnumerable<ProviderSourceDataset> sourceDatasets, IEnumerable<TestScenarioResult> currentResults, SpecModel.SpecificationSummary specification, BuildProject buildProject);
    }
}
