using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Graph.Controllers
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
        public async Task<IActionResult> UpsertCalculations([FromBody]Calculation[] calculations)
        {
            return await _graphService.UpsertCalculations(calculations);
        }

        [Route("api/graph/specifications")]
        [HttpPost]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertSpecifications([FromBody]Specification[] specifications)
        {
            return await _graphService.UpsertSpecifications(specifications);
        }

        [Route("api/graph/calculation/{calculationId}")]
        [HttpDelete]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculation([FromRoute]string calculationId)
        {
            return await _graphService.DeleteCalculation(calculationId);
        }

        [Route("api/graph/specification/{specificationId}")]
        [HttpDelete]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteSpecification([FromRoute]string specificationId)
        {
            return await _graphService.DeleteSpecification(specificationId);
        }

        [Route("api/graph/specification/{specificationId}/relationships/calculation/{calculationId}")]
        [HttpPut]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculationSpecificationRelationship([FromRoute]string calculationId, [FromRoute]string specificationId)
        {
            return await _graphService.UpsertCalculationSpecificationRelationship(calculationId, specificationId);
        }

        [Route("api/graph/calculation/{calculationIdA}/relationships/calculation/{calculationIdB}")]
        [HttpPut]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculationCalculationRelationship([FromRoute]string calculationIdA, [FromRoute]string calculationIdB)
        {
            return await _graphService.UpsertCalculationCalculationRelationship(calculationIdA, calculationIdB);
        }

        [Route("api/graph/calculation/{calculationId}/relationships/calculations")]
        [HttpPost]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculationCalculationsRelationships([FromRoute]string calculationId, [FromBody]string[] calculationIds)
        {
            return await _graphService.UpsertCalculationCalculationsRelationships(calculationId, calculationIds);
        }

        [Route("api/graph/specification/{specificationId}/relationships/calculation/{calculationId}/")]
        [HttpDelete]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculationSpecificationRelationship([FromRoute]string calculationId, [FromRoute]string specificationId)
        {
            return await _graphService.DeleteCalculationSpecificationRelationship(calculationId, specificationId);
        }

        [Route("api/graph/calculation/{calculationIdA}/relationships/calculation/{calculationIdB}")]
        [HttpDelete]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculationCalculationRelationship([FromRoute]string calculationIdA, [FromRoute]string calculationIdB)
        {
            return await _graphService.DeleteCalculationCalculationRelationship(calculationIdA, calculationIdB);
        }

        [HttpDelete("api/graph/specification/{specificationId}/all")]
        [ProducesResponseType((int) HttpStatusCode.OK)]
        public async Task<IActionResult> DeleteAllForSpecification([FromRoute] string specificationId)
        {
            return await _graphService.DeleteAllForSpecification(specificationId);
        }
    }
}
