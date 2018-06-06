using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestResultsRepository
    {
        Task<IEnumerable<TestScenarioResult>> GetCurrentTestResults(IEnumerable<string> providerIds, string specificationId);

        Task<HttpStatusCode> SaveTestProviderResults(IEnumerable<TestScenarioResult> providerResult);

        Task<IEnumerable<DocumentEntity<TestScenarioResult>>> GetAllTestResults();

        Task<ProviderTestScenarioResultCounts> GetProviderCounts(string providerId);

        Task<SpecificationTestScenarioResultCounts> GetSpecificationCounts(string specificationId);
    }
}
