using CalculateFunding.Models.Results;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestResultsRepository
    {
        Task<IEnumerable<TestScenarioResult>> GetCurrentTestResults(IEnumerable<string> providerIds, string specificationId);

        Task<HttpStatusCode> SaveTestProviderResults(IEnumerable<TestScenarioResult> providerResult);
    }
}
