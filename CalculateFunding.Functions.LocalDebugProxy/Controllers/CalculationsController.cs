using CalculateFunding.Services.Calcs.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{
    public class CalculationsController : BaseController
    {
        private readonly ICalculationService _calcsService;
        private readonly IPreviewService _previewService;

        public CalculationsController(ICalculationService calcsService, IPreviewService previewService, IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _calcsService = calcsService;
            _previewService = previewService;
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

        [Route("api/calcs/calculation-current-version")]
        [HttpGet]
        public Task<IActionResult> RunCalculationCurrentVersion()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _calcsService.GetCalculationCurrentVersion(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/calculation-save-version")]
        [HttpPost]
        public Task<IActionResult> RunCalculationSaveVersion()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _calcsService.SaveCalculationVersion(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/calculation-version-history")]
        [HttpGet]
        public Task<IActionResult> RunCalculationVersions()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _calcsService.GetCalculationHistory(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/calculation-versions")]
        [HttpPost]
        public Task<IActionResult> RunCalculationCompareVersions()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _calcsService.GetCalculationVersions(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/compile-preview")]
        [HttpPost]
        public Task<IActionResult> RunCompilePreview()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _previewService.Compile(ControllerContext.HttpContext.Request);
        }
    }
}
