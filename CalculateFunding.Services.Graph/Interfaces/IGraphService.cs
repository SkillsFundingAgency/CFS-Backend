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
        Task<IActionResult> UpsertCalculationDataFieldsRelationships(string calculationId, string[] datasetFieldIds);
        Task<IActionResult> UpsertCalculationDataFieldRelationship(string calculationId, string datasetFieldId);
        Task<IActionResult> DeleteCalculationSpecificationRelationship(string calculationId, string specificationId);
        Task<IActionResult> DeleteCalculationCalculationRelationship(string calculationIdA, string calculationIdB);
        Task<IActionResult> UpsertDataset(Dataset dataset);
        Task<IActionResult> UpsertDatasets(Dataset[] datasets);
        Task<IActionResult> DeleteDataset(string datasetId);
        Task<IActionResult> UpsertDatasetDefinition(DatasetDefinition definition);
        Task<IActionResult> UpsertDatasetDefinitions(DatasetDefinition[] definition);
        Task<IActionResult> DeleteDatasetDefinition(string definitionId);
        Task<IActionResult> UpsertDataField(DataField field);
        Task<IActionResult> DeleteDataField(string fieldId);
        Task<IActionResult> UpsertDataFields(IEnumerable<DataField> dataField);
        Task<IActionResult> UpsertDataDefinitionDatasetRelationship(string definitionId, string datasetId);
        Task<IActionResult> DeleteDataDefinitionDatasetRelationship(string definitionId, string datasetId);
        Task<IActionResult> UpsertDatasetDataFieldRelationship(string datasetId, string fieldId);
        Task<IActionResult> DeleteDatasetDataFieldRelationship(string datasetId, string fieldId);
        Task<IActionResult> UpsertSpecificationDatasetRelationship(string specificationId, string datasetId);
        Task<IActionResult> DeleteSpecificationDatasetRelationship(string specificationId, string datasetId);
        Task<IActionResult> DeleteCalculationDataFieldRelationship(string calculationId, string fieldId);

        Task<IActionResult> UpsertFundingLines(FundingLine[] fundingLines);

        Task<IActionResult> DeleteFundingLine(string fieldId);

        Task<IActionResult> UpsertFundingLineCalculationRelationship(string fundingLineId, string calculationId);

        Task<IActionResult> UpsertCalculationFundingLineRelationship(string calculationId, string fundingLineId);

        Task<IActionResult> DeleteFundingLineCalculationRelationship(string fundingLineId, string calculationId);

        Task<IActionResult> DeleteCalculationFundingLineRelationship(string calculationId, string fundingLineId);

        Task<IActionResult> GetCalculationCircularDependencies(string specificationId);
        Task<IActionResult> GetAllEntities<TNode>(string nodeId)
            where TNode:class;

        Task<IActionResult> DeleteFundingLines(string[] fieldIds);
        Task<IActionResult> UpsertFundingLineCalculationRelationships(params (string fundingLineId, string calculationId)[] relationships);
        Task<IActionResult> UpsertCalculationFundingLineRelationships(params (string calculationId, string fundingLineId)[] relationships);
        Task<IActionResult> DeleteFundingLineCalculationRelationships(params (string fundingLineId, string calculationId)[] relationships);
        Task<IActionResult> DeleteCalculationFundingLineRelationships(params (string calculationId, string fundingLineId)[] relationships);
        Task<IActionResult> DeleteSpecificationDatasetRelationships(params (string specificationId, string datasetId)[] relationships);
        Task<IActionResult> UpsertCalculationDataFieldRelationships(params (string calculationId, string fieldId)[] relationships);
        Task<IActionResult> DeleteCalculationDataFieldRelationships(params (string calculationId, string fieldId)[] relationships);
        Task<IActionResult> DeleteCalculations(params string[] calculationIds);
        Task<IActionResult> UpsertCalculationSpecificationRelationships(params (string calculationId, string specificationId)[] relationships);
        Task<IActionResult> DeleteCalculationDatasetFieldRelationship(string calculationId, string datasetFieldId);
        Task<IActionResult> DeleteDatasetField(string datasetFieldId);
        Task<IActionResult> UpsertSpecificationDatasetRelationships(params (string specificationId, string datasetId)[] relationships);
        Task<IActionResult> DeleteCalculationSpecificationRelationships(params (string calculationId, string specificationId)[] relationships);
        Task<IActionResult> DeleteCalculationCalculationRelationships(params (string calculationIdA, string calculationIdB)[] relationships);
        Task<IActionResult> UpsertDatasetDataFieldRelationships(params (string datasetId, string fieldId)[] relationships);
        Task<IActionResult> UpsertDataDefinitionDatasetRelationships(params (string definitionId, string datasetId)[] relationships);
        Task<IActionResult> GetAllCalculationsForAll(params string[] calculationIds);
        Task<IActionResult> GetAllFundingLinesForAll(params string[] fundingLineIds);
    }
}
