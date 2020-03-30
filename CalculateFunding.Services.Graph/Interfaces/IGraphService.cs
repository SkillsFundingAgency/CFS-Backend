using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Graph;

namespace CalculateFunding.Services.Graph.Interfaces
{
    public interface IGraphService
    {
        Task<IActionResult> UpsertCalculations(IEnumerable<Calculation> calculations);
        Task<IActionResult> UpsertSpecifications(IEnumerable<Specification> specifications);
        Task<IActionResult> DeleteCalculation(string calculationId);
        Task<IActionResult> DeleteSpecification(string specificationId);
        Task<IActionResult> UpsertCalculationSpecificationRelationship(string calculationId, string specificationId);
        Task<IActionResult> UpsertCalculationCalculationRelationship(string calculationIdA, string calculationIdB);
        Task<IActionResult> UpsertCalculationCalculationsRelationships(string calculationId, string[] calculationIds);
        Task<IActionResult> DeleteCalculationSpecificationRelationship(string calculationId, string specificationId);
        Task<IActionResult> DeleteCalculationCalculationRelationship(string calculationIdA, string calculationIdB);
        Task<IActionResult> DeleteAllForSpecification(string specificationId);
        Task<IActionResult> UpsertDataset(Dataset dataset);
        Task<IActionResult> DeleteDataset(string datasetId);
        Task<IActionResult> UpsertDatasetDefinition(DatasetDefinition definition);
        Task<IActionResult> DeleteDatasetDefinition(string definitionId);
        Task<IActionResult> UpsertDataField(DataField field);
        Task<IActionResult> DeleteDataField(string fieldId);
        Task<IActionResult> UpsertDataDefinitionDatasetRelationship(string definitionId, string datasetId);
        Task<IActionResult> DeleteDataDefinitionDatasetRelationship(string definitionId, string datasetId);
        Task<IActionResult> UpsertDatasetDataFieldRelationship(string datasetId, string fieldId);
        Task<IActionResult> DeleteDatasetDataFieldRelationship(string datasetId, string fieldId);
        Task<IActionResult> CreateSpecificationDatasetRelationship(string specificationId, string datasetId);
        Task<IActionResult> DeleteSpecificationDatasetRelationship(string specificationId, string datasetId);
        Task<IActionResult> CreateCalculationDataFieldRelationship(string calculationId, string fieldId);
        Task<IActionResult> DeleteCalculationDataFieldRelationship(string calculationId, string fieldId);
        Task<IActionResult> GetCalculationCircularDependencies(string specificationId);
        Task<IActionResult> GetAllEntities<TNode>(string nodeId)
            where TNode:class;
    }
}
