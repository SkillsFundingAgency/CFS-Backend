using CalculateFunding.Functions.Calcs.Http;
using CalculateFunding.Services.Calcs.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{
    public class CalculationsController : Controller
    {
        private readonly ICalculationService _calcsService;

        public CalculationsController(ICalculationService calcsService)
        {
            _calcsService = calcsService;
        }

        [Route("api/calcs/calculations-search")]
        [HttpPost]
        public Task<IActionResult> RunSpecificationsByYear()
        {
            return _calcsService.SearchCalculations(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/calculation-by-id")]
        [HttpGet]
        public Task<IActionResult> RunCalculationById()
        {
            return _calcsService.GetCalculationById(ControllerContext.HttpContext.Request);
        }
    }
}
