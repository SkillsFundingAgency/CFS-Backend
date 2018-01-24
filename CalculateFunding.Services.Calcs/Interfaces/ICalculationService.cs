using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationService
    {
        Task CreateCalculation(Message message);

        Task<IActionResult> SearchCalculations(HttpRequest request);

        Task<IActionResult> GetCalculationById(HttpRequest request);

        Task<IActionResult> GetCompareVersions(HttpRequest request);

        Task<IActionResult> GetCalculationHistory(HttpRequest request);
    }
}
