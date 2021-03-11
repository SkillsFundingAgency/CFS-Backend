using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using CalculateFunding.Services.Graph.Models;
using Microsoft.AspNetCore.Mvc;
using Enum = CalculateFunding.Models.Graph.Enum;

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
        
        [HttpPost("api/graph/dataset")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertDataset([FromBody]Dataset dataset)
        {
            return await _graphService.UpsertDataset(dataset);
        }

        [HttpPost("api/graph/datasets")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertDatasets([FromBody]Dataset[] datasets)
        {
            return await _graphService.UpsertDatasets(datasets);
        }

        [HttpDelete("api/graph/datasets/{datasetId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteDataset([FromRoute]string definitionId)
        {
            return await _graphService.DeleteDataset(definitionId);
        }
        
        [HttpPost("api/graph/datasetdefinition")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertDatasetDefinition([FromBody]DatasetDefinition definition)
        {
            return await _graphService.UpsertDatasetDefinition(definition);
        }

        [HttpPost("api/graph/datasetdefinitions")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertDatasetDefinitions([FromBody]DatasetDefinition[] definitions)
        {
            return await _graphService.UpsertDatasetDefinitions(definitions);
        }

        [HttpDelete("api/graph/datasetdefinitions/{definitionId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteDatasetDefinition([FromRoute]string definitionId)
        {
            return await _graphService.DeleteDatasetDefinition(definitionId);
        }
        
        [HttpPost("api/graph/datafield")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertDataField([FromBody]DataField field)
        {
            return await _graphService.UpsertDataField(field);
        }

        [HttpPost("api/graph/datafields")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertDataFields([FromBody]DataField[] fields)
        {
            return await _graphService.UpsertDataFields(fields);
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
        
        [HttpPost("api/graph/datasetdefinitions/relationships/datasets")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertDataDefinitionDatasetRelationships([FromBody] AmendRelationshipRequest[] relationships)
        {
            return await _graphService.UpsertDataDefinitionDatasetRelationships(ToRelationships(relationships));
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
        
        [HttpPost("api/graph/datasets/relationships/datafields")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertDatasetDataFieldRelationships([FromBody] AmendRelationshipRequest[] relationships)
        {
            return await _graphService.UpsertDatasetDataFieldRelationships(ToRelationships(relationships));
        }
        
        [HttpDelete("api/graph/datasets/{datasetId}/relationships/datafields/{fieldId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteDatasetDataFieldRelationship([FromRoute]string datasetId, [FromRoute]string fieldId)
        {
            return await _graphService.DeleteDatasetDataFieldRelationship(datasetId, fieldId);
        }
        
        [HttpPut("api/graph/specifications/{specificationId}/relationships/datasets/{datasetId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertSpecificationDatasetRelationship([FromRoute]string specificationId, [FromRoute]string datasetId)
        {
            return await _graphService.UpsertSpecificationDatasetRelationship(specificationId, datasetId);
        }
        
        [HttpPut("api/graph/specifications/relationships/datasets")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertSpecificationDatasetRelationships([FromBody] AmendRelationshipRequest[] relationships)
        {
            return await _graphService.UpsertSpecificationDatasetRelationships(ToRelationships(relationships));
        }
        
        [HttpDelete("api/graph/specifications/{specificationId}/relationships/datasets/{datasetId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteSpecificationDatasetRelationship([FromRoute]string specificationId, [FromRoute]string datasetId)
        {
            return await _graphService.DeleteSpecificationDatasetRelationship(specificationId, datasetId);
        }

        [HttpPost("api/graph/specifications/relationships/datasets/delete")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteSpecificationsDatasetRelationships([FromBody] AmendRelationshipRequest[] relationships)
        {
            return await _graphService.DeleteSpecificationDatasetRelationships(ToRelationships(relationships));
        }
        
        [HttpPut("api/graph/calculations/{calculationId}/relationships/datafields/{fieldId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculationDataFieldRelationship([FromRoute]string calculationId, [FromRoute]string fieldId)
        {
            return await _graphService.UpsertCalculationDataFieldRelationship(calculationId, fieldId);
        }
        
        [HttpPut("api/graph/calculations/relationships/datafields")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculationDataFieldRelationships([FromBody] AmendRelationshipRequest[] relationships)
        {
            return await _graphService.UpsertCalculationDataFieldRelationships(ToRelationships(relationships));
        }
        
        [HttpDelete("api/graph/calculations/{calculationId}/relationships/datafields/{fieldId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculationDataFieldRelationship([FromRoute]string calculationId, [FromRoute]string fieldId)
        {
            return await _graphService.DeleteCalculationDataFieldRelationship(calculationId, fieldId);
        }

        [HttpDelete("api/graph/calculations/{calculationId}/relationships/enums/{fieldId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculationEnumRelationship([FromRoute] string calculationId, [FromRoute] string fieldId)
        {
            return await _graphService.DeleteCalculationEnumRelationship(calculationId, fieldId);
        }

        [HttpPost("api/graph/calculations/relationships/datafields/delete")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculationDataFieldRelationships([FromBody] AmendRelationshipRequest[] relationships)
        {
            return await _graphService.DeleteCalculationDataFieldRelationships(ToRelationships(relationships));
        }

        [HttpPost("api/graph/calculations/relationships/enums/delete")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculationEnumRelationships([FromBody] AmendRelationshipRequest[] relationships)
        {
            return await _graphService.DeleteCalculationEnumRelationships(ToRelationships(relationships));
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

        [HttpPost("api/graph/enums")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertEnums([FromBody] Enum[] enums)
        {
            return await _graphService.UpsertEnums(enums);
        }

        [HttpDelete("api/graph/calculation/{calculationId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculation([FromRoute]string calculationId)
        {
            return await _graphService.DeleteCalculation(calculationId);
        }
        
        [HttpPost("api/graph/calculation/delete")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculations([FromBody]string[] calculationIds)
        {
            return await _graphService.DeleteCalculations(calculationIds);
        }

        [HttpDelete("api/graph/specification/{specificationId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteSpecification([FromRoute]string specificationId)
        {
            return await _graphService.DeleteSpecification(specificationId);
        }

        [HttpDelete("api/graph/enum/{enumId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteEnum([FromRoute] string enumId)
        {
            return await _graphService.DeleteEnum(enumId);
        }

        [HttpPut("api/graph/specification/{specificationId}/relationships/calculation/{calculationId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculationSpecificationRelationship([FromRoute]string calculationId, [FromRoute]string specificationId)
        {
            return await _graphService.UpsertCalculationSpecificationRelationship(calculationId, specificationId);
        }
        
        [HttpPost("api/graph/specification/relationships/calculation")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculationSpecificationRelationships([FromBody] AmendRelationshipRequest[] relationships)
        {
            return await _graphService.UpsertCalculationSpecificationRelationships(ToRelationships(relationships));
        }

        [HttpGet("api/graph/calculation/circulardependencies/{specificationId}")]
        public async Task<IActionResult> GetCalculationCircularDependencies([FromRoute]string specificationId)
        {
            return await _graphService.GetCalculationCircularDependencies(specificationId);
        }

        [HttpGet("api/graph/specification/getallentities/{specificationId}")]
        public async Task<IActionResult> GetAllEntitiesForSpecification([FromRoute]string specificationId)
        {
            return await _graphService.GetAllEntities<Specification>(specificationId);
        }

        [HttpGet("api/graph/enum/getallentities/{enumId}")]
        public async Task<IActionResult> GetAllEntitiesForEnum([FromRoute] string enumId)
        {
            return await _graphService.GetAllEntities<Enum>(enumId);
        }

        [HttpGet("api/graph/calculation/getallentities/{calculationId}")]
        public async Task<IActionResult> GetAllEntitiesForCalculation([FromRoute]string calculationId)
        {
            return await _graphService.GetAllEntities<Calculation>(calculationId);
        }
        
        [HttpPost("api/graph/calculation/getallentitiesforall")]
        public async Task<IActionResult> GetAllEntitiesForAllCalculations([FromBody]string[] calculationIds)
        {
            return await _graphService.GetAllCalculationsForAll(calculationIds);
        }

        [HttpGet("api/graph/dataset/getallentities/{datafieldid}")]
        public async Task<IActionResult> GetAllEntitiesForDataset([FromRoute] string datafieldId)
        {
            return await _graphService.GetAllEntities<DataField>(datafieldId);
        }

        [HttpGet("api/graph/fundingline/getallentities/{fundingLineId}")]
        public async Task<IActionResult> GetAllEntitiesForFundingLine([FromRoute] string fundingLineId)
        {
            return await _graphService.GetAllEntities<FundingLine>(fundingLineId);
        }

        [HttpPost("api/graph/fundingline/getallentitiesforall")]
        public async Task<IActionResult> GetAllEntitiesForAllFundingLines([FromBody] string[] fundingLineIds)
        {
            return await _graphService.GetAllFundingLinesForAll(fundingLineIds);
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
        
        [HttpPost("api/graph/specification/relationships/calculation/delete")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculationSpecificationRelationships([FromBody] AmendRelationshipRequest[] relationships)
        {
            return await _graphService.DeleteCalculationSpecificationRelationships(ToRelationships(relationships));
        }

        [HttpDelete("api/graph/calculation/{calculationIdA}/relationships/calculation/{calculationIdB}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculationCalculationRelationship([FromRoute]string calculationIdA, [FromRoute]string calculationIdB)
        {
            return await _graphService.DeleteCalculationCalculationRelationship(calculationIdA, calculationIdB);
        }
        
        [HttpDelete("api/graph/calculation/{calculationIdA}/relationships/calculation/{calculationIdB}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculationCalculationRelationships([FromBody] AmendRelationshipRequest[] relationships)
        {
            return await _graphService.DeleteCalculationCalculationRelationships(ToRelationships(relationships));
        }
       
        [HttpPost("api/graph/calculation/{calculationId}/relationships/datafields")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculationDataFieldsRelationships([FromRoute]string calculationId, [FromBody]string[] dataFieldIds)
        {
            return await _graphService.UpsertCalculationDataFieldsRelationships(calculationId, dataFieldIds);
        }

        [HttpPut("api/graph/calculation/relationships/enum")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculationEnumRelationships([FromBody] AmendRelationshipRequest[] relationships)
        {
            return await _graphService.UpsertCalculationEnumRelationships(ToRelationships(relationships));
        }

        [HttpPost("api/graph/fundinglines")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertFundingLines([FromBody]FundingLine[] fundingLines)
        {
            return await _graphService.UpsertFundingLines(fundingLines);
        }

        [HttpDelete("api/graph/fundingline/{fieldId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteFundingLine([FromRoute]string fieldId)
        {
            return await _graphService.DeleteFundingLine(fieldId);
        }
        
        [HttpPost("api/graph/fundingline/delete")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteFundingLines([FromBody]string[] fieldIds)
        {
            return await _graphService.DeleteFundingLines(fieldIds);
        }

        [HttpPost("api/graph/enum/delete")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteEnums([FromBody] string[] fieldIds)
        {
            return await _graphService.DeleteEnums(fieldIds);
        }

        [HttpPut("api/graph/fundingline/{fundingLineId}/relationships/calculation/{calculationId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertFundingLineCalculationRelationship([FromRoute]string fundingLineId, [FromRoute]string calculationId)
        {
            return await _graphService.UpsertFundingLineCalculationRelationship(fundingLineId, calculationId);
        }

        [HttpPut("api/graph/calculation/{calculationId}/relationships/enum/{enumId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculationEnumRelationship([FromRoute] string calculationId, [FromRoute] string enumId)
        {
            return await _graphService.UpsertCalculationEnumRelationship(calculationId, enumId);
        }

        [HttpPut("api/graph/fundingline/relationships/calculation")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertFundingLineCalculationRelationships([FromBody] AmendRelationshipRequest[] relationships)
        {
            return await _graphService.UpsertFundingLineCalculationRelationships(ToRelationships(relationships));
        }

        [HttpPut("api/graph/calculation/{calculationId}/relationships/fundingline/{fundingLineId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculationFundingLineRelationship([FromRoute]string calculationId, [FromRoute]string fundingLineId)
        {
            return await _graphService.UpsertCalculationFundingLineRelationship(calculationId, fundingLineId);
        }
        
        [HttpPost("api/graph/calculation/relationships/fundingline")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> UpsertCalculationFundingLineRelationships([FromBody] AmendRelationshipRequest[] relationships)
        {
            return await _graphService.UpsertCalculationFundingLineRelationships(ToRelationships(relationships));
        }

        [HttpDelete("api/graph/fundingline/{fundingLineId}/relationships/calculation/{calculationId}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteFundingLineCalculationRelationship([FromRoute]string fundingLineId, [FromRoute]string calculationId)
        {
            return await _graphService.DeleteFundingLineCalculationRelationship(fundingLineId, calculationId);
        }
        
        [HttpDelete("api/graph/fundingline/relationships/calculation")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteFundingLineCalculationRelationships([FromBody] AmendRelationshipRequest[] relationships)
        {
            return await _graphService.DeleteFundingLineCalculationRelationships(ToRelationships(relationships));
        }

        [HttpDelete("api/graph/calculation/{calculationId}/relationships/fundingline/{fundingline}")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculationFundingLineRelationship([FromRoute]string calculationId, [FromRoute]string fundingLineId)
        {
            return await _graphService.DeleteCalculationFundingLineRelationship(calculationId, fundingLineId);
        }
        
        [HttpPost("api/graph/calculation/relationships/fundingline/delete")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> DeleteCalculationFundingLineRelationships([FromBody] AmendRelationshipRequest[] relationships)
        {
            return await _graphService.DeleteCalculationFundingLineRelationships(ToRelationships(relationships));
        }

        private static (string IdA, string IdB)[] ToRelationships(AmendRelationshipRequest[] relationships)
            => relationships.Select(_ => (_.IdA, _.IdB)).ToArray();
    }
}
