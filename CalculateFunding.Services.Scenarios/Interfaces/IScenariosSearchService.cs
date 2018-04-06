using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface IScenariosSearchService
    {
        Task<IActionResult> SearchScenarios(HttpRequest request);
    }
}
