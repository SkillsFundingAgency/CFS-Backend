using CalculateFunding.Models;
using CalculateFunding.Repositories.Common.Search;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Interfaces
{
    public interface ITestResultsSearchService
    {
        Task<IActionResult> SearchTestScenarioResults(SearchModel searchModel);

        Task<TestScenarioSearchResults> SearchTestScenarioResultsInternal(SearchModel searchModel);
    }
}
