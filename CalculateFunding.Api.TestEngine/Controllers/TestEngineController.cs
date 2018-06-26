using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Api.TestEngine.Controllers
{
    public class TestEngineController : Controller
    {
        private readonly IGherkinParserService _gherkinParserService;
        private readonly ITestResultsSearchService _testResultsSearchService;
        private readonly ITestEngineService _testEngineService;
        private readonly ITestResultsCountsService _testResultsCountsService;
        private readonly ITestResultsService _testResultsService;

        public TestEngineController(
            IGherkinParserService gherkinParserService,
            ITestResultsSearchService testResultsSearchService,
            ITestEngineService testEngineService,
            ITestResultsCountsService testResultsCountsService,
            ITestResultsService testResultsService)
        {
            Guard.ArgumentNotNull(gherkinParserService, nameof(gherkinParserService));
            Guard.ArgumentNotNull(testResultsSearchService, nameof(testResultsSearchService));
            Guard.ArgumentNotNull(testEngineService, nameof(testEngineService));
            Guard.ArgumentNotNull(testResultsCountsService, nameof(testResultsCountsService));
            Guard.ArgumentNotNull(testResultsService, nameof(testResultsService));

            _gherkinParserService = gherkinParserService;
            _testResultsSearchService = testResultsSearchService;
            _testEngineService = testEngineService;
            _testResultsCountsService = testResultsCountsService;
            _testResultsService = testResultsService;
        }

        [Route("api/tests/validate-test")]
        [HttpPost]
        public Task<IActionResult> RunScenariosSearch()
        {
            return _gherkinParserService.ValidateGherkin(ControllerContext.HttpContext.Request);
        }

        [Route("api/tests/testscenario-search")]
        [HttpPost]
        public Task<IActionResult> RunSearchTestScenarioResults()
        {
            return _testResultsSearchService.SearchTestScenarioResults(ControllerContext.HttpContext.Request);
        }

        [Route("api/tests/run-tests")]
        [HttpPost]
        public Task<IActionResult> RunTests()
        {
            return _testEngineService.RunTests(ControllerContext.HttpContext.Request);
        }

        [Route("api/tests/get-result-counts")]
        [HttpPost]
        public Task<IActionResult> RunGetResultCounts()
        {
            return _testResultsCountsService.GetResultCounts(ControllerContext.HttpContext.Request);
        }

        [Route("api/tests/get-testscenario-result-counts-for-provider")]
        [HttpGet]
        public Task<IActionResult> RunGetProviderStatusCountsForTestScenario()
        {
            return _testResultsCountsService.GetTestScenarioCountsForProvider(ControllerContext.HttpContext.Request);
        }

        [Route("api/tests/testscenario-reindex")]
        [HttpGet]
        public Task<IActionResult> RunReindex()
        {
            return _testResultsService.ReIndex(ControllerContext.HttpContext.Request);
        }

        [Route("api/tests/get-testscenario-result-counts-for-specifications")]
        [HttpPost]
        public Task<IActionResult> RunGetGetTestScenarioCountsForSpecifications()
        {
            return _testResultsCountsService.GetTestScenarioCountsForSpecifications(ControllerContext.HttpContext.Request);
        }

        [Route("api/tests/get-testscenario-result-counts-for-specification-for-provider")]
        [HttpGet]
        public Task<IActionResult> RunGetTestScenarioCountsForProviderForSpecification()
        {
            return _testResultsCountsService.GetTestScenarioCountsForProviderForSpecification(ControllerContext.HttpContext.Request);
        }
    }
}
