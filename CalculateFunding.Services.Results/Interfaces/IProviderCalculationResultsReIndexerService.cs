using CalculateFunding.Common.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderCalculationResultsReIndexerService
    {
        Task<IActionResult> ReIndexCalculationResults(string correlationId, Reference user);

        Task ReIndexCalculationResults(Message message);
    }
}
