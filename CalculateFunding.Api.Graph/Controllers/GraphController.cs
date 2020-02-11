using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Datasets.Controllers
{
    public class GraphController : ControllerBase
    {
        private readonly IGraphService _graphService;

        public GraphController(IGraphService graphService)
        {
            Guard.ArgumentNotNull(graphService, nameof(graphService));

            _graphService = graphService;
        }

        [Route("api/graph/calculations")]
        [HttpPost]
        [ProducesResponseType(200)]
        public async Task<IActionResult> SaveCalculations([FromBody]Calculation[] calculations)
        {
            return await _graphService.SaveCalculations(calculations);
        }

        [Route("api/graph/calculation/{calculationId}")]
        [HttpDelete]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculation([FromRoute]string calculationId)
        {
            return await _graphService.DeleteCalculation(calculationId);
        }
    }
}
