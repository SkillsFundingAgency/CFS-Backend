using System;
using System.Threading.Tasks;
using CalculateFunding.Functions.Specs.Http;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{

    public class SpecificationsController : BaseController
    {
        private readonly ISpecificationsService _specService;

        public SpecificationsController(ISpecificationsService specService, IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _specService = specService;
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

        [Route("api/specs/academic-years")]
        [HttpGet]
        public Task<IActionResult> RunAcademicYears()
        {
            return AcademicYears.Run(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/commands/academic-years")]
        [HttpPost]
        [HttpDelete]
        public Task<IActionResult> RunAcademicYearsCommands([FromBody]string value)
        {
            return AcademicYears.RunCommands(ControllerContext.HttpContext.Request, null);
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

        [Route("api/specs/commands/funding-streams")]
        [HttpPost]
        [HttpDelete]
        public Task<IActionResult> RunFundingStreamsCommands([FromBody]string value)
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return FundingStreams.RunCommands(ControllerContext.HttpContext.Request, null);
        }
    }
}
