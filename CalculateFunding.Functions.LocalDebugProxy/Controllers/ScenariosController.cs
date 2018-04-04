using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Scenarios.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{
    public class ScenariosController : BaseController
    {
        private readonly IScenariosService _scenarioService;
        private readonly IScenariosSearchService _scenariosSearchService;

        public ScenariosController(IServiceProvider serviceProvider, 
            IScenariosService scenarioService, IScenariosSearchService scenariosSearchService) : base(serviceProvider)
        {
            Guard.ArgumentNotNull(scenarioService, nameof(scenarioService));
            Guard.ArgumentNotNull(scenariosSearchService, nameof(scenariosSearchService));

            _scenarioService = scenarioService;
            _scenariosSearchService = scenariosSearchService;
        }

        [Route("api/scenarios/save-scenario-test-version")]
        [HttpPost]
        public Task<IActionResult> RunSaveScenarioTestVersion()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _scenarioService.SaveVersion(ControllerContext.HttpContext.Request);
        }

        [Route("api/scenarios/scenarios-search")]
        [HttpPost]
        public Task<IActionResult> RunScenariosSearch()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _scenariosSearchService.SearchScenarios(ControllerContext.HttpContext.Request);
        }

        [Route("api/scenarios/get-scenarios-by-specificationId")]
        [HttpGet]
        public Task<IActionResult> RunGetTestScenariosBySpecificationId()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _scenarioService.GetTestScenariosBySpecificationId(ControllerContext.HttpContext.Request);
        }
    }
}
