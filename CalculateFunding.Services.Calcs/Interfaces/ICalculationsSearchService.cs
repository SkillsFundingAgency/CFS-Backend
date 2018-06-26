using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationsSearchService
    {
        Task<IActionResult> SearchCalculations(HttpRequest request);
    }
}
