using CalculateFunding.Common.Models;
using CalculateFunding.Services.Processing.Interfaces;
using CalculateFunding.Services.Results.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface ICalculationResultQADatabasePopulationService : IJobProcessingService
    {
        Task<IActionResult> QueueCalculationResultQADatabasePopulationJob(
            PopulateCalculationResultQADatabaseRequest populateCalculationResultQADatabaseRequest,
            Reference user,
            string correlationId);
    }
}
