using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.Testing;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.TestEngine.Controllers
{
    public class TestEngineController : ControllerBase
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
        [Produces(typeof(IEnumerable<GherkinError>))]
        public Task<IActionResult> RunValidateGherkin([FromBody] ValidateGherkinRequestModel validateGherkinRequestModel)
        {
            return _gherkinParserService.ValidateGherkin(validateGherkinRequestModel);
        }

        [Route("api/tests/testscenario-search")]
        [HttpPost]
        [Produces(typeof(TestScenarioSearchResults))]
        public Task<IActionResult> RunSearchTestScenarioResults([FromBody] SearchModel searchModel)
        {
            return _testResultsSearchService.SearchTestScenarioResults(searchModel);
        }

        [Route("api/tests/run-tests")]
        [HttpPost]
        [Produces(typeof(IEnumerable<TestScenarioResult>))]
        public Task<IActionResult> RunTests([FromBody] TestExecutionModel testExecutionModel)
        {
            return _testEngineService.RunTests(testExecutionModel);
        }

        [Route("api/tests/get-result-counts")]
        [HttpPost]
        [Produces(typeof(IEnumerable<TestScenarioResultCounts>))]
        public Task<IActionResult> RunGetResultCounts([FromBody] TestScenariosResultsCountsRequestModel testScenariosResultsCountsRequestModel)
        {
            return _testResultsCountsService.GetResultCounts(testScenariosResultsCountsRequestModel);
        }

        [Route("api/tests/get-testscenario-result-counts-for-provider")]
        [HttpGet]
        [Produces(typeof(ProviderTestScenarioResultCounts))]
        public Task<IActionResult> RunGetProviderStatusCountsForTestScenario([FromQuery] string providerId)
        {
            return _testResultsCountsService.GetTestScenarioCountsForProvider(providerId);
        }

        [Route("api/tests/testscenario-reindex")]
        [HttpGet]
        [ProducesResponseType(204)]
        public Task<IActionResult> RunReindex()
        {
            return _testResultsService.ReIndex();
        }

        [Route("api/tests/get-testscenario-result-counts-for-specifications")]
        [HttpPost]
        [Produces(typeof(IEnumerable<SpecificationTestScenarioResultCounts>))]
        public Task<IActionResult> RunGetTestScenarioCountsForSpecifications([FromBody] SpecificationListModel specificationListModel)
        {
            return _testResultsCountsService.GetTestScenarioCountsForSpecifications(specificationListModel);
        }

        [Route("api/tests/get-testscenario-result-counts-for-specification-for-provider")]
        [HttpGet]
        [Produces(typeof(ScenarioResultCounts))]
        public Task<IActionResult> RunGetTestScenarioCountsForProviderForSpecification([FromQuery] string providerId, [FromQuery] string specificationId)
        {
            return _testResultsCountsService.GetTestScenarioCountsForProviderForSpecification(providerId, specificationId);
        }
    }
}
