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
        private readonly ITestEngineService _testEngineService;

        public TestRunnerController(
            IServiceProvider serviceProvider, 
            IGherkinParserService gherkinParserService,
            ITestResultsSearchService testResultsSearchService,
            ITestEngineService testEngineService
            ) : base(serviceProvider)
        {
            _gherkinParserService = gherkinParserService;
            _testResultsSearchService = testResultsSearchService;
            _testEngineService = testEngineService;
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

        [Route("api/tests/run-tests")]
        [HttpPost]
        public Task<IActionResult> RunTests()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _testEngineService.RunTests(ControllerContext.HttpContext.Request);
        }
    }
}
