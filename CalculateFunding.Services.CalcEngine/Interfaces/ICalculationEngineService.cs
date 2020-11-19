using CalculateFunding.Services.Processing.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine.Interfaces
{
    public interface ICalculationEngineService : IJobProcessingService
    {
        Task<IActionResult> GenerateAllocations(HttpRequest request);
    }
}
