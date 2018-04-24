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
        private readonly ITestResultsCountsService _testResultsCountsService;

        public TestRunnerController(
            IServiceProvider serviceProvider, 
            IGherkinParserService gherkinParserService,
            ITestResultsSearchService testResultsSearchService,
            ITestEngineService testEngineService,
            ITestResultsCountsService testResultsCountsService) : base(serviceProvider)
        {
            _gherkinParserService = gherkinParserService;
            _testResultsSearchService = testResultsSearchService;
            _testEngineService = testEngineService;
            _testResultsCountsService = testResultsCountsService;
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

        [Route("api/tests/get-result-counts")]
        [HttpPost]
        public Task<IActionResult> RunGetResultCounts()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _testResultsCountsService.GetResultCounts(ControllerContext.HttpContext.Request);
        }
    }
}
