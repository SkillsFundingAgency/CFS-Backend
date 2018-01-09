using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Functions.Common;
using CalculateFunding.Functions.Specs.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{

    public class SpecificationsController : Controller
    {
        [Route("api/specs/specifications")]
        [HttpGet]
        public async Task<IActionResult>RunSpecifications()
        {
            return await Specifications.Run(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/specifications-by-year")]
        [HttpGet]
        public async Task<IActionResult> RunSpecificationsByYear()
        {
            return await Specifications.RunSpecificationsByYear(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/specifications")]
        [HttpPost]
        public async Task<IActionResult> RunSpecificationsCommands([FromBody]string value)
        {
            return await Specifications.RunCreateSpecification(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/academic-years")]
        [HttpGet]
        public async Task<IActionResult> RunAcademicYears()
        {
            return await AcademicYears.Run(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/commands/academic-years")]
        [HttpPost]
        [HttpDelete]
        public async Task<IActionResult> RunAcademicYearsCommands([FromBody]string value)
        {
            return await AcademicYears.RunCommands(ControllerContext.HttpContext.Request, null);
        }


        [Route("api/specs/funding-streams")]
        [HttpGet]
        public async Task<IActionResult> RunFundingStreams()
        {
            return await FundingStreams.Run(ControllerContext.HttpContext.Request, null);
        }

        [Route("api/specs/commands/funding-streams")]
        [HttpPost]
        [HttpDelete]
        public async Task<IActionResult> RunFundingStreamsCommands([FromBody]string value)
        {
            return await FundingStreams.RunCommands(ControllerContext.HttpContext.Request, null);
        }


    }
}
