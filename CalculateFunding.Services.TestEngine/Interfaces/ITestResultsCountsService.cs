using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Scenarios;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestResultsCountsService
    {
        Task<IActionResult> GetResultCounts(TestScenariosResultsCountsRequestModel testScenariosResultsCountsRequestModel);

        Task<IActionResult> GetTestScenarioCountsForProvider(string providerId);

        Task<IActionResult> GetTestScenarioCountsForSpecifications(SpecificationListModel specificationListModel);

        Task<IActionResult> GetTestScenarioCountsForProviderForSpecification(string providerId, string specificationId);
    }
}
