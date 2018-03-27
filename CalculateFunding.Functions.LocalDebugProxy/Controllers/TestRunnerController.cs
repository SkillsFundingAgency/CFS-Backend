using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{
    public class TestRunnerController : BaseController
    {
        private readonly IGherkinParserService _gherkinParserService;

        public TestRunnerController(IServiceProvider serviceProvider, IGherkinParserService gherkinParserService) : base(serviceProvider)
        {
            _gherkinParserService = gherkinParserService;
        }

        [Route("api/tests/validate-test")]
        [HttpPost]
        public Task<IActionResult> RunScenariosSearch()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _gherkinParserService.ValidateGherkin(ControllerContext.HttpContext.Request);
        }
    }
}
