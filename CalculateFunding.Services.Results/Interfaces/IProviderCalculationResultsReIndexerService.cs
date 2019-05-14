using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderCalculationResultsReIndexerService
    {
        Task<IActionResult> ReIndexCalculationResults(HttpRequest httpRequest);

        Task ReIndexCalculationResults(Message message);
    }
}
