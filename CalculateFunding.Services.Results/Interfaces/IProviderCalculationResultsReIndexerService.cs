using CalculateFunding.Common.Models;
using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderCalculationResultsReIndexerService : IProcessingService
    {
        Task<IActionResult> ReIndexCalculationResults(string correlationId, Reference user);
    }
}
