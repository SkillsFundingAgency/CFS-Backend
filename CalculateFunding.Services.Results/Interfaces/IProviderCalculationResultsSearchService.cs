using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderCalculationResultsSearchService
    {
        Task<IActionResult> SearchCalculationProviderResults(HttpRequest request);
    }
}
