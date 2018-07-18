using CalculateFunding.Models;
using CalculateFunding.Repositories.Common.Search;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestResultsSearchService
    {
        Task<IActionResult> SearchTestScenarioResults(HttpRequest request);

        Task<TestScenarioSearchResults> SearchTestScenarioResults(SearchModel searchModel);
    }
}
