using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{
    public class DatasetsController : BaseController
    {
        private readonly IDefinitionsService _definitionService;

        public DatasetsController(IServiceProvider serviceProvider, IDefinitionsService definitionService) 
            : base (serviceProvider)
        {
            _definitionService = definitionService;
        }

        [Route("api/datasets/data-definitions")]
        [HttpPost]
        public Task<IActionResult> RunDataDefinitions()
        {
            SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

            return _definitionService.SaveDefinition(ControllerContext.HttpContext.Request);
        }
    }
}
