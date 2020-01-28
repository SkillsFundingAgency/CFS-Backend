using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Scenarios.Controllers
{
    public class ScenariosController : ControllerBase
    {
        private readonly IScenariosService _scenarioService;
        private readonly IScenariosSearchService _scenariosSearchService;

        public ScenariosController(
            IScenariosService scenarioService,
            IScenariosSearchService scenariosSearchService)
        {
            Guard.ArgumentNotNull(scenarioService, nameof(scenarioService));
            Guard.ArgumentNotNull(scenariosSearchService, nameof(scenariosSearchService));

            _scenarioService = scenarioService;
            _scenariosSearchService = scenariosSearchService;
        }

        [Route("api/scenarios/save-scenario-test-version")]
        [HttpPost]
        [Produces(typeof(CurrentTestScenario))]
        public Task<IActionResult> RunSaveScenarioTestVersion([FromBody] CreateNewTestScenarioVersion createNewTestScenarioVersion)
        {
            Reference user = ControllerContext.HttpContext.Request.GetUser();
            string correlationId = ControllerContext.HttpContext.Request.GetCorrelationId();

            return _scenarioService.SaveVersion(createNewTestScenarioVersion, user, correlationId);
        }

        [Route("api/scenarios/scenarios-search")]
        [HttpPost]
        [Produces(typeof(ScenarioSearchResults))]
        public Task<IActionResult> RunScenariosSearch([FromBody] SearchModel searchModel)
        {
            return _scenariosSearchService.SearchScenarios(searchModel);
        }

        [Route("api/scenarios/get-scenarios-by-specificationId")]
        [HttpGet]
        [Produces(typeof(IEnumerable<TestScenario>))]
        public Task<IActionResult> RunGetTestScenariosBySpecificationId([FromQuery] string specificationId)
        {
            return _scenarioService.GetTestScenariosBySpecificationId(specificationId);
        }

        [Route("api/scenarios/get-scenario-by-id")]
        [HttpGet]
        [Produces(typeof(TestScenario))]
        public Task<IActionResult> RunGetTestScenarioById([FromQuery] string scenarioId)
        {
            return _scenarioService.GetTestScenarioById(scenarioId);
        }

        [Route("api/scenarios/scenarios-search-reindex")]
        [HttpPost]
        [Produces(typeof(string))]
        public Task<IActionResult> RunScenariosSearchReindex()
        {
            return _scenariosSearchService.ReIndex();
        }

        [Route("api/scenarios/get-current-scenario-by-id")]
        [HttpGet]
        [Produces(typeof(CurrentTestScenario))]
        public Task<IActionResult> RunGetCurrentTestScenarioById([FromQuery] string scenarioId)
        {
            return _scenarioService.GetCurrentTestScenarioById(scenarioId);
        }
    }
}
