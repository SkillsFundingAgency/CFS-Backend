using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{
    public class TestRunnerController : BaseController
    {
        private readonly IGherkinParserService _gherkinParserService;
        private readonly ITestResultsSearchService _testResultsSearchService;

        public TestRunnerController(
            IServiceProvider serviceProvider, 
            IGherkinParserService gherkinParserService,
            ITestResultsSearchService testResultsSearchService
            ) : base(serviceProvider)
        {
            _gherkinParserService = gherkinParserService;
            _testResultsSearchService = testResultsSearchService;
        }

        [Route("api/tests/validate-test")]
        [HttpPost]
        public Task<IActionResult> RunScenariosSearch()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _gherkinParserService.ValidateGherkin(ControllerContext.HttpContext.Request);
        }

        [Route("api/tests/testscenario-search")]
        [HttpPost]
        public Task<IActionResult> RunSearchTestScenarioResults()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _testResultsSearchService.SearchTestScenarioResults(ControllerContext.HttpContext.Request);
        }
    }
}
