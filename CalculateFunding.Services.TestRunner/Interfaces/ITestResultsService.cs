using CalculateFunding.Models.Results;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestResultsService
    {
        Task<HttpStatusCode> SaveTestProviderResults(IEnumerable<TestScenarioResult> testResults);

    }
}
