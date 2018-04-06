using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{

    public class SpecificationsController : BaseController
    {
        private readonly ISpecificationsService _specService;
        private readonly ISpecificationsSearchService _specSearchService;

        public SpecificationsController(ISpecificationsService specService, 
           ISpecificationsSearchService specSearchService,  IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _specService = specService;
            _specSearchService = specSearchService;
        }

        [Route("api/specs/specifications")]
        [HttpGet]
        public Task<IActionResult>RunSpecifications()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetSpecificationById(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications-by-year")]
        [HttpGet]
        public Task<IActionResult> RunSpecificationsByYear()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetSpecificationByAcademicYearId(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specification-by-name")]
        [HttpGet]
        public Task<IActionResult> RunSpecificationByName()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetSpecificationByName(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/specifications")]
        [HttpPost]
        public Task<IActionResult> RunSpecificationsCommands([FromBody]string value)
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.CreateSpecification(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/funding-streams")]
        [HttpGet]
        public Task<IActionResult> RunFundingStreams()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetFundingStreams(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/policy-by-name")]
        [HttpPost]
        public Task<IActionResult> RunPolicyByName()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetPolicyByName(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/policies")]
        [HttpPost]
        public Task<IActionResult> RunCreatePolicy()
        {
            return _specService.CreatePolicy(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/calculation-by-name")]
        [HttpPost]
        public Task<IActionResult> RunCalculationByName()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetCalculationByName(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/calculation-by-id")]
        [HttpGet]
        public Task<IActionResult> RunCalculationBySpecificationIdAndCalculationId()
        {
            return _specService.GetCalculationBySpecificationIdAndCalculationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/calculations-by-specificationid")]
        [HttpGet]
        public Task<IActionResult> RunCalculationsBySpecificationId()
        {
            return _specService.GetCalculationsBySpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/calculations")]
        [HttpPost]
        public Task<IActionResult> RunCreateCalculation()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.CreateCalculation(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/allocation-lines")]
        [HttpGet]
        public Task<IActionResult> RunAllocationLines()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.GetAllocationLines(ControllerContext.HttpContext.Request);
        }

        [Route("api/specs/reindex")]
        [HttpGet]
        public Task<IActionResult> ReIndex()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specService.ReIndex();
        }

        [Route("api/specs/specifications-search")]
        [HttpPost]
        public Task<IActionResult> RunSearchSpecifications()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _specSearchService.SearchSpecifications(ControllerContext.HttpContext.Request);
        }
    }
}
