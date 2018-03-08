using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IResultsService
    {
	    Task UpdateProviderData(EventData message);
	    Task<IActionResult> GetProviderResults(HttpRequest request);
	    Task<IActionResult> GetProviderSpecifications(HttpRequest request);
        Task<IActionResult> GetProviderById(HttpRequest request);
        Task<IActionResult> GetProviderResultsBySpecificationId(HttpRequest request);
        Task<IActionResult> UpdateProviderResults(HttpRequest request);
    }
}
