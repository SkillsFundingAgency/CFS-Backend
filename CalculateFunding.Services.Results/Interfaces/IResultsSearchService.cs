using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IResultsSearchService
    {
        Task<IActionResult> SearchProviders(HttpRequest request);
	    Task<IActionResult> GetProviderResults(HttpRequest httpContextRequest);
    }
}
