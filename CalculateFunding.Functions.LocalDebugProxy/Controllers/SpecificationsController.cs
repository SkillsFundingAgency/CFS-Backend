using System.Threading.Tasks;
using CalculateFunding.Functions.Specs.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{

    public class SpecificationsController : Controller
    {
        /// <summary>
        /// Get Specification By ID
        /// </summary>
        /// <returns></returns>
        [Route("api/specs/specifications")]
        [HttpGet]
        public Task<IActionResult>RunSpecifications()
        {
            return Specifications.Run(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/specifications-by-year")]
        [HttpGet]
        public Task<IActionResult> RunSpecificationsByYear()
        {
            return Specifications.RunSpecificationsByYear(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/specification-by-name")]
        [HttpGet]
        public Task<IActionResult> RunSpecificationByName()
        {
            return Specifications.RunSpecificationByName(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/specifications")]
        [HttpPost]
        public Task<IActionResult> RunSpecificationsCommands([FromBody]string value)
        {
            return Specifications.RunCreateSpecification(ControllerContext.HttpContext.Request, null);
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
            return FundingStreams.Run(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/policy-by-name")]
        [HttpPost]
        public Task<IActionResult> RunPolicyByName()
        {
            return Policies.RunPolicyByName(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/policies")]
        [HttpPost]
        public Task<IActionResult> RunCreatePolicy()
        {
            return Policies.RunCreatePolicy(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/calculation-by-name")]
        [HttpPost]
        public Task<IActionResult> RunCalculationByName()
        {
            return Calculations.RunCalculationByName(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/calculation-by-id")]
        [HttpGet]
        public Task<IActionResult> RunCalculationBySpecificationIdAndCalculationId()
        {
            return Calculations.RunCalculationBySpecificationIdAndCalculationId(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/calculations")]
        [HttpPost]
        public Task<IActionResult> RunCreateCalculation()
        {
            return Calculations.RunCreateCalculation(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/allocation-lines")]
        [HttpGet]
        public Task<IActionResult> RunAllocationLines()
        {
            return Calculations.RunAllocationLines(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/commands/funding-streams")]
        [HttpPost]
        [HttpDelete]
        public Task<IActionResult> RunFundingStreamsCommands([FromBody]string value)
        {
            return FundingStreams.RunCommands(ControllerContext.HttpContext.Request, null);
        }


    }
}
