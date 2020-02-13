using System.Collections.Generic;
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
        public async Task<IActionResult> SaveCalculations([FromBody]Calculation[] calculations)
        {
            return await _graphService.SaveCalculations(calculations);
        }

        [Route("api/graph/specifications")]
        [HttpPost]
        [ProducesResponseType(200)]
        public async Task<IActionResult> SaveSpecifications([FromBody]Specification[] specifications)
        {
            return await _graphService.SaveSpecifications(specifications);
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

        [Route("api/graph/calculation/specification/{calculationId}/{specificationId}")]
        [HttpPut]
        [ProducesResponseType(200)]
        public async Task<IActionResult> CreateCalculationSpecificationRelationship([FromRoute]string calculationId, [FromRoute]string specificationId)
        {
            return await _graphService.CreateCalculationSpecificationRelationship(calculationId, specificationId);
        }

        [Route("api/graph/calculation/calculation/{calculationIdA}/{calculationIdB}")]
        [HttpPut]
        [ProducesResponseType(200)]
        public async Task<IActionResult> CreateCalculationCalculationRelationship([FromRoute]string calculationIdA, [FromRoute]string calculationIdB)
        {
            return await _graphService.CreateCalculationCalculationRelationship(calculationIdA, calculationIdB);
        }


        [Route("api/graph/calculations/delete/specification/{calculationId}/{specificationId}")]
        [HttpPut]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculationSpecificationRelationship([FromRoute]string calculationId, [FromRoute]string specificationId)
        {
            return await _graphService.DeleteCalculationSpecificationRelationship(calculationId, specificationId);
        }

        [Route("api/graph/calculation/delete/calculation/{calculationIdA}/{calculationIdB}")]
        [HttpPut]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculationCalculationRelationship([FromRoute]string calculationIdA, [FromRoute]string calculationIdB)
        {
            return await _graphService.DeleteCalculationCalculationRelationship(calculationIdA, calculationIdB);
        }
    }
}
