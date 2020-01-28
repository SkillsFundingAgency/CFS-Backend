using CalculateFunding.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface IScenariosSearchService
    {
        Task<IActionResult> SearchScenarios(SearchModel searchModel);

        Task<IActionResult> ReIndex();
    }
}
