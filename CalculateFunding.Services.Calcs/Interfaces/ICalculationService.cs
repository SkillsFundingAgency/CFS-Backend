using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationService
    {
        Task CreateCalculation(EventData message);

        Task<IActionResult> GetCalculationById(HttpRequest request);

        Task<IActionResult> GetCalculationVersions(HttpRequest request);

        Task<IActionResult> GetCalculationHistory(HttpRequest request);

        Task<IActionResult> GetCalculationCurrentVersion(HttpRequest request);

        Task<IActionResult> SaveCalculationVersion(HttpRequest request);

        Task<IActionResult> PublishCalculationVersion(HttpRequest request);

        Task<IActionResult> GetCalculationCodeContext(HttpRequest request);
    }
}
