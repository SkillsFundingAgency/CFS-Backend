using CalculateFunding.Services.Calcs.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Api.Calcs.Controllers
{
    public class CalculationsController : Controller
    {
        private readonly ICalculationService _calcsService;
        private readonly IPreviewService _previewService;
        private readonly ICalculationsSearchService _calcsSearchService;
        private readonly IBuildProjectsService _buildProjectsService;

        public CalculationsController(
            ICalculationService calcsService,
            ICalculationsSearchService calcsSearchService,
            IPreviewService previewService,
            IBuildProjectsService buildProjectsService)
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
            return _calcsSearchService.SearchCalculations(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/calculation-by-id")]
        [HttpGet]
        public Task<IActionResult> RunCalculationById()
        {
            return _calcsService.GetCalculationById(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/calculation-summaries-for-specification")]
        [HttpGet]
        public Task<IActionResult> RunGetCalculationSummariesForSpecification()
        {
            return _calcsService.GetCalculationSummariesForSpecification(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/current-calculations-for-specification")]
        [HttpGet]
        public Task<IActionResult> RunGetCurrentCalculationsForSpecification()
        {
            return _calcsService.GetCurrentCalculationsForSpecification(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/calculation-current-version")]
        [HttpGet]
        public Task<IActionResult> RunCalculationCurrentVersion()
        {
            return _calcsService.GetCalculationCurrentVersion(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/calculation-save-version")]
        [HttpPost]
        public Task<IActionResult> RunCalculationSaveVersion()
        {
            return _calcsService.SaveCalculationVersion(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/calculation-version-history")]
        [HttpGet]
        public Task<IActionResult> RunCalculationVersionHistory()
        {
            return _calcsService.GetCalculationHistory(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/calculation-versions")]
        [HttpPost]
        public Task<IActionResult> RunCalculationVersions()
        {
            return _calcsService.GetCalculationVersions(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/calculation-edit-status")]
        [HttpPut]
        public Task<IActionResult> RunCalculationEditStatus()
        {
            return _calcsService.UpdateCalculationStatus(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/compile-preview")]
        [HttpPost]
        public Task<IActionResult> RunCompilePreview()
        {
            return _previewService.Compile(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/get-buildproject-by-specification-id")]
        [HttpGet]
        public Task<IActionResult> RunGetBuildProjectBySpecificationId()
        {
            return _buildProjectsService.GetBuildProjectBySpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/get-calculation-code-context")]
        [HttpGet]
        public Task<IActionResult> RunGetCalculationCodeContext()
        {
            return _calcsService.GetCalculationCodeContext(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/update-buildproject-relationships")]
        [HttpPost]
        public Task<IActionResult> RunUpdateBuildProjectRealtionships()
        {
            return _buildProjectsService.UpdateBuildProjectRelationships(ControllerContext.HttpContext.Request);
        }

        [Route("api/calcs/reindex")]
        [HttpGet]
        public Task<IActionResult> RunCalculationReIndex()
        {
            return _calcsService.ReIndex();
        }

        [Route("api/calcs/status-counts")]
        [HttpPost]
        public Task<IActionResult> RunGetCalculationStatusCounts()
        {
            return _calcsService.GetCalculationStatusCounts(ControllerContext.HttpContext.Request);
        }
    }
}
