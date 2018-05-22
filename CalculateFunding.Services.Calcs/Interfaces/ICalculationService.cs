using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Azure.ServiceBus;
using CalculateFunding.Models.Results;
using System.Collections.Generic;
using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.Calcs.Interfaces
{
    public interface ICalculationService
    {
        Task CreateCalculation(Message message);

        Task<IActionResult> GetCalculationById(HttpRequest request);

        Task<IActionResult> GetCurrentCalculationsForSpecification(HttpRequest request);

        Task<IActionResult> GetCalculationSummariesForSpecification(HttpRequest request);


        Task<IActionResult> GetCalculationVersions(HttpRequest request);

        Task<IActionResult> GetCalculationHistory(HttpRequest request);

        Task<IActionResult> GetCalculationCurrentVersion(HttpRequest request);

        Task<IActionResult> SaveCalculationVersion(HttpRequest request);

        Task<IActionResult> PublishCalculationVersion(HttpRequest request);

        Task<IActionResult> GetCalculationCodeContext(HttpRequest request);

        Task<IActionResult> ReIndex();

        Task<BuildProject> CreateBuildProject(string specificationId, IEnumerable<Calculation> calculations);

        Task UpdateCalculationsForSpecification(Message message);

        Task UpdateCalculationsForCalculationSpecificationChange(Message message);
    }
}
