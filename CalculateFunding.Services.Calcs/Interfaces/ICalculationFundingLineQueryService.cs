using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationFundingLineQueryService
    {
        Task<IActionResult> GetCalculationFundingLines(string calculationId);
    }
}