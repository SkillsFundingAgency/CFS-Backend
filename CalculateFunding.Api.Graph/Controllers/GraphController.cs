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
        
        [HttpPost("api/graph/datasets")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertDataset([FromBody]Dataset dataset)
        {
            return await _graphService.UpsertDataset(dataset);
        }
        
        [HttpDelete("api/graph/datasets/{datasetId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteDataset([FromRoute]string definitionId)
        {
            return await _graphService.DeleteDataset(definitionId);
        }
        
        [HttpPost("api/graph/datasetdefinitions")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertDatasetDefinition([FromBody]DatasetDefinition definition)
        {
            return await _graphService.UpsertDatasetDefinition(definition);
        }
        
        [HttpDelete("api/graph/datasetdefinitions/{definitionId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteDatasetDefinition([FromRoute]string definitionId)
        {
            return await _graphService.DeleteDatasetDefinition(definitionId);
        }
        
        [HttpPost("api/graph/datafields")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertDataFields([FromBody]DataField fields)
        {
            return await _graphService.UpsertDataField(fields);
        }
        
        [HttpDelete("api/graph/datafields/{fieldId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteDataField([FromRoute]string fieldId)
        {
            return await _graphService.DeleteDataField(fieldId);
        }
        
        [HttpPut("api/graph/datasetdefinitions/{definitionId}/relationships/datasets/{datasetId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertDataDefinitionDatasetRelationship([FromRoute]string definitionId, [FromRoute]string datasetId)
        {
            return await _graphService.UpsertDataDefinitionDatasetRelationship(definitionId, datasetId);
        }
        
        [HttpDelete("api/graph/datasetdefinitions/{definitionId}/relationships/datasets/{datasetId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteDataDefinitionDatasetRelationship([FromRoute]string definitionId, [FromRoute]string datasetId)
        {
            return await _graphService.DeleteDataDefinitionDatasetRelationship(definitionId, datasetId);
        }
        
        [HttpPut("api/graph/datasets/{datasetId}/relationships/datafields/{fieldId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertDatasetDataFieldRelationship([FromRoute]string datasetId, [FromRoute]string fieldId)
        {
            return await _graphService.UpsertDatasetDataFieldRelationship(datasetId, fieldId);
        }
        
        [HttpDelete("api/graph/datasets/{datasetId}/relationships/datafields/{fieldId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteDatasetDataFieldRelationship([FromRoute]string datasetId, [FromRoute]string fieldId)
        {
            return await _graphService.DeleteDatasetDataFieldRelationship(datasetId, fieldId);
        }
        
        [HttpPut("api/graph/specifications/{specificationId}/relationships/datasets/{datasetId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> CreateSpecificationDatasetRelationship([FromRoute]string specificationId, [FromRoute]string datasetId)
        {
            return await _graphService.CreateSpecificationDatasetRelationship(specificationId, datasetId);
        }
        
        [HttpDelete("api/graph/specifications/{specificationId}/relationships/datasets/{datasetId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteSpecificationDatasetRelationship([FromRoute]string specificationId, [FromRoute]string datasetId)
        {
            return await _graphService.DeleteSpecificationDatasetRelationship(specificationId, datasetId);
        }
        
        [HttpPut("api/graph/calculations/{calculationId}/relationships/datafields/{fieldId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> CreateCalculationDataFieldRelationship([FromRoute]string calculationId, [FromRoute]string fieldId)
        {
            return await _graphService.CreateCalculationDataFieldRelationship(calculationId, fieldId);
        }
        
        [HttpDelete("api/graph/calculations/{calculationId}/relationships/datafields/{fieldId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculationDataFieldRelationship([FromRoute]string calculationId, [FromRoute]string fieldId)
        {
            return await _graphService.DeleteCalculationDataFieldRelationship(calculationId, fieldId);
        }

        [HttpPost("api/graph/calculations")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculations([FromBody]Calculation[] calculations)
        {
            return await _graphService.UpsertCalculations(calculations);
        }

        [HttpPost("api/graph/specifications")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertSpecifications([FromBody]Specification[] specifications)
        {
            return await _graphService.UpsertSpecifications(specifications);
        }

        [HttpDelete("api/graph/calculation/{calculationId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculation([FromRoute]string calculationId)
        {
            return await _graphService.DeleteCalculation(calculationId);
        }

        [HttpDelete("api/graph/specification/{specificationId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteSpecification([FromRoute]string specificationId)
        {
            return await _graphService.DeleteSpecification(specificationId);
        }

        [HttpPut("api/graph/specification/{specificationId}/relationships/calculation/{calculationId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculationSpecificationRelationship([FromRoute]string calculationId, [FromRoute]string specificationId)
        {
            return await _graphService.UpsertCalculationSpecificationRelationship(calculationId, specificationId);
        }

        [HttpPut("api/graph/calculation/{calculationIdA}/relationships/calculation/{calculationIdB}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculationCalculationRelationship([FromRoute]string calculationIdA, [FromRoute]string calculationIdB)
        {
            return await _graphService.UpsertCalculationCalculationRelationship(calculationIdA, calculationIdB);
        }

        [HttpPost("api/graph/calculation/{calculationId}/relationships/calculations")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculationCalculationsRelationships([FromRoute]string calculationId, [FromBody]string[] calculationIds)
        {
            return await _graphService.UpsertCalculationCalculationsRelationships(calculationId, calculationIds);
        }

        [HttpDelete("api/graph/specification/{specificationId}/relationships/calculation/{calculationId}/")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculationSpecificationRelationship([FromRoute]string calculationId, [FromRoute]string specificationId)
        {
            return await _graphService.DeleteCalculationSpecificationRelationship(calculationId, specificationId);
        }

        [HttpDelete("api/graph/calculation/{calculationIdA}/relationships/calculation/{calculationIdB}")]
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
