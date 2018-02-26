using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IResultsService
    {
	    Task UpdateProviderData(Message message);
	    Task<IActionResult> GetProviderResults(HttpRequest httpContextRequest);
	    Task<IActionResult> GetProviderSpecifications(HttpRequest req);
    }
}
