using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Scenarios.Controllers
{
    public class ScenariosController : Controller
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
        public Task<IActionResult> RunSaveScenarioTestVersion()
        {
            return _scenarioService.SaveVersion(ControllerContext.HttpContext.Request);
        }

        [Route("api/scenarios/scenarios-search")]
        [HttpPost]
        [Produces(typeof(ScenarioSearchResults))]
        public Task<IActionResult> RunScenariosSearch()
        {
            return _scenariosSearchService.SearchScenarios(ControllerContext.HttpContext.Request);
        }

        [Route("api/scenarios/get-scenarios-by-specificationId")]
        [HttpGet]
        [Produces(typeof(IEnumerable<TestScenario>))]
        public Task<IActionResult> RunGetTestScenariosBySpecificationId()
        {
            return _scenarioService.GetTestScenariosBySpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/scenarios/get-scenario-by-id")]
        [HttpGet]
        [Produces(typeof(TestScenario))]
        public Task<IActionResult> RunGetTestScenarioById()
        {
            return _scenarioService.GetTestScenarioById(ControllerContext.HttpContext.Request);
        }

        [Route("api/scenarios/scenarios-search-reindex")]
        [HttpPost]
        [Produces(typeof(string))]
        public Task<IActionResult> RunScenariosSearchReindex()
        {
            return _scenariosSearchService.ReIndex(ControllerContext.HttpContext.Request);
        }

        [Route("api/scenarios/get-current-scenario-by-id")]
        [HttpGet]
        [Produces(typeof(CurrentTestScenario))]
        public Task<IActionResult> RunGetCurrentTestScenarioById()
        {
            return _scenarioService.GetCurrentTestScenarioById(ControllerContext.HttpContext.Request);
        }
    }
}
