using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results.Interfaces
{
    public interface IProviderCalculationResultsReIndexerService
    {
        Task<IActionResult> ReIndex();
    }
}
