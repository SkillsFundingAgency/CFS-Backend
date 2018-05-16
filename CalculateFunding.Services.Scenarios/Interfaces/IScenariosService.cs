using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    public interface IScenariosService
    {
        Task<IActionResult> SaveVersion(HttpRequest request);

        Task<IActionResult> GetTestScenariosBySpecificationId(HttpRequest request);

        Task<IActionResult> GetTestScenarioById(HttpRequest request);

        Task<IActionResult> GetCurrentTestScenarioById(HttpRequest request);

        Task UpdateScenarioForSpecification(Message message);
    }
}
