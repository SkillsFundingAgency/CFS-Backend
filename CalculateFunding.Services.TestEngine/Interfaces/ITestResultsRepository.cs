using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Results;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestResultsRepository
    {
        Task<IEnumerable<TestScenarioResult>> GetCurrentTestResults(IEnumerable<string> providerIds, string specificationId);

        Task DeleteCurrentTestScenarioTestResults(IEnumerable<TestScenarioResult> testScenarioResults);

        Task<HttpStatusCode> SaveTestProviderResults(IEnumerable<TestScenarioResult> providerResult);

        Task<IEnumerable<DocumentEntity<TestScenarioResult>>> GetAllTestResults();

        Task<ProviderTestScenarioResultCounts> GetProviderCounts(string providerId);

        Task<SpecificationTestScenarioResultCounts> GetSpecificationCounts(string specificationId);

        Task<ScenarioResultCounts> GetProvideCountForSpecification(string providerId, string specificationId);
    }
}
