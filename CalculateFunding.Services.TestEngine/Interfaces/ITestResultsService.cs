using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Scenarios;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestResultsService
    {
        Task<HttpStatusCode> SaveTestProviderResults(IEnumerable<TestScenarioResult> testResults, IEnumerable<ProviderResult> providerResults);

        Task<IActionResult> ReIndex();

        Task CleanupTestResultsForSpecificationProviders(Message message);

        Task UpdateTestResultsForSpecification(Message message);

        Task<IActionResult> DeleteTestResults(Message message);
    }
}
