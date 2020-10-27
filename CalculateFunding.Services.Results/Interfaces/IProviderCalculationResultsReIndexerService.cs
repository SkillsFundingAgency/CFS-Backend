using CalculateFunding.Common.Models;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderCalculationResultsReIndexerService : IProcessingService
    {
        Task<IActionResult> ReIndexCalculationResults(string correlationId, Reference user);
    }
}
