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
        private readonly ICalculationsSearchService _calcsSearchService;
        private readonly IBuildProjectsService _buildProjectsService;

        public CalculationsController(ICalculationService calcsService, ICalculationsSearchService calcsSearchService, 
            IPreviewService previewService, IServiceProvider serviceProvider, IBuildProjectsService buildProjectsService)
            : base(serviceProvider)
        {
            _calcsService = calcsService;
            _previewService = previewService;
            _calcsSearchService = calcsSearchService;
            _buildProjectsService = buildProjectsService;
        }

        [Route("api/calcs/calculations-search")]
        [HttpPost]
        public Task<IActionResult> RunCalculationsSearch()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _calcsSearchService.SearchCalculations(ControllerContext.HttpContext.Request);
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
        public Task<IActionResult> RunCalculationVersionHistory()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _calcsService.GetCalculationHistory(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/calculation-versions")]
        [HttpPost]
        public Task<IActionResult> RunCalculationVersions()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _calcsService.GetCalculationVersions(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/calculation-publish")]
        [HttpPost]
        public Task<IActionResult> RunCalculationPublish()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _calcsService.PublishCalculationVersion(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/compile-preview")]
        [HttpPost]
        public Task<IActionResult> RunCompilePreview()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _previewService.Compile(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/get-buildproject-by-specification-id")]
        [HttpGet]
        public Task<IActionResult> RunGetBuildProjectBySpecificationId()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _buildProjectsService.GetBuildProjectBySpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/get-calculation-code-context")]
        [HttpGet]
        public Task<IActionResult> RunGetCalculationCodeContext()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _calcsService.GetCalculationCodeContext(ControllerContext.HttpContext.Request);
        }
    }
}
