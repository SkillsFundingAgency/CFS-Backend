using CalculateFunding.Services.Calcs.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{
    public class CalculationsController : BaseController
    {
        private readonly ICalculationService _calcsService;

        public CalculationsController(ICalculationService calcsService, IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _calcsService = calcsService;
        }

        [Route("api/calcs/calculations-search")]
        [HttpPost]
        public Task<IActionResult> RunSpecificationsByYear()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _calcsService.SearchCalculations(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/calculation-by-id")]
        [HttpGet]
        public Task<IActionResult> RunCalculationById()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _calcsService.GetCalculationById(ControllerContext.HttpContext.Request);
        }
    }
}
