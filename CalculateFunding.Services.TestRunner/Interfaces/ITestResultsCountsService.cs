using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestResultsCountsService
    {
        Task<IActionResult> GetResultCounts(HttpRequest request);

        Task<IActionResult> GetTestScenarioCountsForProvider(HttpRequest request);

        Task<IActionResult> GetTestScenarioCountsForSpecifications(HttpRequest request);
    }
}
