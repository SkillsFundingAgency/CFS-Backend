using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using Enum = CalculateFunding.Models.Graph.Enum;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Common.Graph.Interfaces;
using Serilog;
using Microsoft.AspNetCore.Mvc;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Graph;

namespace CalculateFunding.Services.Graph
{
    public class GraphService : IGraphService
    {
        private readonly ILogger _logger;
        private readonly ICalculationRepository _calcRepository;
        private readonly ISpecificationRepository _specRepository;
        private readonly IEnumRepository _enumRepository;
        private readonly IDatasetRepository _datasetRepository;
        private readonly IFundingLineRepository _fundingLineRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly Polly.AsyncPolicy _cachePolicy;
        private const int PageSize = 100;

        public GraphService(ILogger logger, 
            ICalculationRepository calcRepository, 
            ISpecificationRepository specRepository, 
            IDatasetRepository datasetRepository,
            IFundingLineRepository fundingLineRepository,
            IEnumRepository enumRepository,
            IGraphResiliencePolicies resiliencePolicies,
            ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(calcRepository, nameof(calcRepository));
            Guard.ArgumentNotNull(specRepository, nameof(specRepository));
            Guard.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
            Guard.ArgumentNotNull(fundingLineRepository, nameof(fundingLineRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(enumRepository, nameof(enumRepository));

            _logger = logger;
            _calcRepository = calcRepository;
            _specRepository = specRepository;
            _datasetRepository = datasetRepository;
            _fundingLineRepository = fundingLineRepository;
            _enumRepository = enumRepository;
            _cacheProvider = cacheProvider;
            _cachePolicy = resiliencePolicies.CacheProviderPolicy;
        }

        public async Task<IActionResult> UpsertDataset(Dataset dataset)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.UpsertDataset(dataset),
                $"Unable to create dataset {dataset.AsJson()}");
        }

        public async Task<IActionResult> UpsertDatasets(Dataset[] datasets)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.UpsertDatasets(datasets),
                $"Unable to create datasets {datasets.AsJson()}");
        }

        public async Task<IActionResult> DeleteDataset(string datasetId)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.DeleteDataset(datasetId),
                $"Unable to delete dataset {datasetId}");
        }
        
        public async Task<IActionResult> UpsertDatasetDefinition(DatasetDefinition definition)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.UpsertDatasetDefinition(definition),
                $"Unable to create dataset definition {definition.AsJson()}");
        }

        public async Task<IActionResult> UpsertDatasetDefinitions(DatasetDefinition[] definitions)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.UpsertDatasetDefinitions(definitions),
                $"Unable to create dataset definitions {definitions.AsJson()}");
        }

        public async Task<IActionResult> DeleteDatasetDefinition(string definitionId)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.DeleteDatasetDefinition(definitionId),
                $"Unable to delete dataset definition {definitionId}");
        }
        
        public async Task<IActionResult> UpsertDataField(DataField field)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.UpsertDataField(field),
                $"Unable to create data field {field.AsJson()}");
        }

        public async Task<IActionResult> DeleteDataField(string fieldId)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.DeleteDataField(fieldId),
                $"Unable to delete data field {fieldId}");
        }

        public async Task<IActionResult> UpsertFundingLines(FundingLine[] fundingLines)
        {
            return await ExecuteRepositoryAction(() => _fundingLineRepository.UpsertFundingLines(fundingLines),
                $"Unable to create funding lines {fundingLines.AsJson()}");
        }

        public async Task<IActionResult> DeleteFundingLine(string fieldId)
        {
            return await ExecuteRepositoryAction(() => _fundingLineRepository.DeleteFundingLine(fieldId),
                $"Unable to delete funding line {fieldId}");
        }
        
        public async Task<IActionResult> DeleteFundingLines(string[] fieldIds)
        {
            return await ExecuteRepositoryAction(() => _fundingLineRepository.DeleteFundingLines(fieldIds),
                $"Unable to delete funding lines.");
        }

        public async Task<IActionResult> UpsertFundingLineCalculationRelationship(string fundingLineId, string calculationId)
        {
            return await ExecuteRepositoryAction(() => _fundingLineRepository.UpsertFundingLineCalculationRelationship(fundingLineId, calculationId),
                $"Unable to create funding line -> calculation relationship {fundingLineId} -> {calculationId}");
        }
        
        public async Task<IActionResult> UpsertFundingLineCalculationRelationships(params (string fundingLineId, string calculationId)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _fundingLineRepository.UpsertFundingLineCalculationRelationships(relationships),
                $"Unable to create funding line -> calculation relationships");
        }
        
        public async Task<IActionResult> UpsertCalculationFundingLineRelationship(string calculationId, string fundingLineId)
        {
            return await ExecuteRepositoryAction(() => _fundingLineRepository.UpsertCalculationFundingLineRelationship(calculationId, fundingLineId),
                $"Unable to create calculation -> funding line relationship {calculationId} -> {fundingLineId}");
        }
        
        public async Task<IActionResult> UpsertCalculationFundingLineRelationships(params (string calculationId, string fundingLineId)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _fundingLineRepository.UpsertCalculationFundingLineRelationships(relationships),
                $"Unable to create calculation -> funding line relationship");
        }

        public async Task<IActionResult> DeleteFundingLineCalculationRelationship(string fundingLineId, string calculationId)
        {
            return await ExecuteRepositoryAction(() => _fundingLineRepository.DeleteFundingLineCalculationRelationship(fundingLineId, calculationId),
                $"Unable to delete funding line -> calculation relationship {fundingLineId} -> {calculationId}");
        }
        
        public async Task<IActionResult> DeleteFundingLineCalculationRelationships(params (string fundingLineId, string calculationId)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _fundingLineRepository.DeleteFundingLineCalculationRelationships(relationships),
                $"Unable to delete funding line -> calculation relationship");
        }

        public async Task<IActionResult> DeleteCalculationFundingLineRelationship(string calculationId, string fundingLineId)
        {
            return await ExecuteRepositoryAction(() => _fundingLineRepository.DeleteCalculationFundingLineRelationship(calculationId, fundingLineId),
                $"Unable to delete calculation -> funding line relationship {calculationId} -> {fundingLineId}");
        }
        
        public async Task<IActionResult> DeleteCalculationFundingLineRelationships(params (string calculationId, string fundingLineId)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _fundingLineRepository.DeleteCalculationFundingLineRelationships(relationships),
                $"Unable to delete calculation -> funding line relationship");
        }

        public async Task<IActionResult> UpsertDatasetRelationship(DatasetRelationship datasetRelationship)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.UpsertDatasetRelationship(datasetRelationship),
                $"Unable to create dataset relationship {datasetRelationship.AsJson()}");
        }

        public async Task<IActionResult> UpsertDatasetRelationships(DatasetRelationship[] datasetRelationships)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.UpsertDatasetRelationships(datasetRelationships),
                $"Unable to create dataset relationship {datasetRelationships.AsJson()}");
        }

        public async Task<IActionResult> DeleteDatasetRelationship(string relationshipId)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.DeleteDatasetRelationship(relationshipId),
                $"Unable to delete dataset relationship {relationshipId}");
        }

        public async Task<IActionResult> DeleteDatasetRelationships(string[] relationshipIds)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.DeleteDatasetRelationships(relationshipIds),
                $"Unable to delete dataset relationships. {relationshipIds.AsJson()}");
        }

        public async Task<IActionResult> GetAllDatasetRelationshipsForAll(string[] relationshipIds)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.GetAllDatasetRelationsshipEntitiesForAll(relationshipIds),
                $"Unable to retrieve all entities for dataset relationship ids {relationshipIds.AsJson()}");
        }

        public async Task<IActionResult> UpsertDatasetRelationshipDataFieldRelationships(string datasetRelationshipId, string[] dataFieldUniqueIds)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.UpsertDatasetRelationshipDataFieldRelationships(
                    dataFieldUniqueIds.Select(_ => (datasetRelationshipId, _)).ToArray()),
                $"Upsert datafield relationship call to dataset relationship failed for dataset relationship:'{datasetRelationshipId}'" +
                $" calling data fields:'{dataFieldUniqueIds.AsJson()}'");
        }

        public async Task<IActionResult> DeleteDatasetRelationshipDataFieldRelationship(string datasetRelationshipId, string dataFieldUniqueId)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.DeleteDatasetRelationshipDataFieldRelationship(datasetRelationshipId, dataFieldUniqueId),
                $"Unable to delete dataset relationship -> data field relationship {datasetRelationshipId} -> {dataFieldUniqueId}");
        }

        public async Task<IActionResult> DeleteDatasetRelationshipDataFieldRelationships(params (string datasetRelationshipId, string dataFieldUniqueId)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.DeleteDatasetRelationshipDataFieldRelationships(relationships),
                $"Delete dataset relationship relationships call to data field failed");
        }

        public async Task<IActionResult> UpsertDataDefinitionDatasetRelationship(string definitionId, string datasetId)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.UpsertDataDefinitionDatasetRelationship(definitionId, datasetId),
                $"Unable to create dataset defition -> dataset relationship {definitionId} -> {datasetId}");
        }
        
        public async Task<IActionResult> UpsertDataDefinitionDatasetRelationships(params (string definitionId, string datasetId)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.UpsertDataDefinitionDatasetRelationships(relationships),
                $"Unable to create dataset defition -> dataset relationships");
        }
        
        public async Task<IActionResult> DeleteDataDefinitionDatasetRelationship(string definitionId, string datasetId)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.DeleteDataDefinitionDatasetRelationship(definitionId, datasetId),
                $"Unable to delete dataset defition -> dataset relationship {definitionId} -> {datasetId}");
        }

        public async Task<IActionResult> UpsertDatasetDataFieldRelationship(string datasetId, string fieldId)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.UpsertDatasetDataFieldRelationship(datasetId, fieldId),
                $"Unable to create dataset -> datafield relationship {datasetId} -> {fieldId}");
        }

        public async Task<IActionResult> UpsertDatasetDataFieldRelationships(params (string datasetId, string fieldId)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.UpsertDatasetDataFieldRelationships(relationships),
                $"Unable to create dataset -> datafield relationships");
        }
        
        public async Task<IActionResult> DeleteDatasetDataFieldRelationship(string datasetId, string fieldId)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.DeleteDatasetDataFieldRelationship(datasetId, fieldId),
                $"Unable to delete dataset -> datafield relationship {datasetId} -> {fieldId}");
        }

        public async Task<IActionResult> UpsertSpecificationDatasetRelationship(string specificationId, string datasetId)
        {
            return await ExecuteRepositoryAction(() => _specRepository.CreateSpecificationDatasetRelationship(specificationId, datasetId),
                $"Unable to create specification -> dataset relationship {specificationId} -> {datasetId}");   
        }
        
        public async Task<IActionResult> UpsertSpecificationDatasetRelationships(params (string specificationId, string datasetId)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _specRepository.CreateSpecificationDatasetRelationships(relationships),
                $"Unable to create specification -> dataset relationships");   
        }
        
        public async Task<IActionResult> DeleteSpecificationDatasetRelationship(string specificationId, string datasetId)
        {
            return await ExecuteRepositoryAction(() => _specRepository.DeleteSpecificationDatasetRelationship(specificationId, datasetId),
                $"Unable to delete specification -> dataset relationship {specificationId} -> {datasetId}");   
        }
        
        public async Task<IActionResult> DeleteSpecificationDatasetRelationships(params (string specificationId, string datasetId)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _specRepository.DeleteSpecificationDatasetRelationships(relationships),
                $"Unable to delete specification -> dataset relationship");   
        }
        
        public async Task<IActionResult> UpsertCalculationDataFieldRelationship(string calculationId, string fieldId)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.UpsertCalculationDataFieldRelationship(calculationId, fieldId),
                $"Unable to create calculation -> datafield relationship {calculationId} -> {fieldId}");   
        }
        
        public async Task<IActionResult> UpsertCalculationDataFieldRelationships(params (string calculationId, string fieldId)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.UpsertCalculationDataFieldRelationships(relationships),
                $"Unable to create calculation -> datafield relationships");   
        }
        
        public async Task<IActionResult> DeleteCalculationDataFieldRelationship(string calculationId, string fieldId)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.DeleteCalculationDataFieldRelationship(calculationId, fieldId),
                $"Unable to delete calculation -> datafield relationship {calculationId} -> {fieldId}");   
        }

        public async Task<IActionResult> DeleteCalculationEnumRelationship(string calculationId, string fieldId)
        {
            return await ExecuteRepositoryAction(() => _enumRepository.DeleteCalculationEnumRelationship(calculationId, fieldId),
                $"Unable to delete calculation -> nume relationship {calculationId} -> {fieldId}");
        }

        public async Task<IActionResult> DeleteCalculationDataFieldRelationships(params (string calculationId, string fieldId)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.DeleteCalculationDataFieldRelationships(relationships),
                $"Unable to delete calculation -> datafield relationships");   
        }

        public async Task<IActionResult> DeleteCalculationEnumRelationships(params (string calculationId, string fieldId)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _enumRepository.DeleteCalculationEnumRelationships(relationships),
                $"Unable to delete calculation -> enum relationships");
        }

        public async Task<IActionResult> DeleteSpecification(string specificationId)
        {
            return await ExecuteRepositoryAction(() => _specRepository.DeleteSpecification(specificationId),
                $"Delete specification failed for specification:'{specificationId}'");
        }

        public async Task<IActionResult> UpsertDataFields(IEnumerable<DataField> dataFields)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.UpsertDataFields(dataFields),
                $"Save datasetfields failed for specifications:'{dataFields.AsJson()}'");
        }

        public async Task<IActionResult> UpsertSpecifications(IEnumerable<Specification> specifications)
        {
            return await ExecuteRepositoryAction(() => _specRepository.UpsertSpecifications(specifications),
                $"Save specifications failed for specifications:'{specifications.AsJson()}'");
        }

        public async Task<IActionResult> UpsertEnums(IEnumerable<Enum> enums)
        {
            return await ExecuteRepositoryAction(() => _enumRepository.UpsertEnums(enums),
                $"Save enums failed for enums:'{enums.AsJson()}'");
        }

        public async Task<IActionResult> DeleteEnum(string enumNameValue)
        {
            return await ExecuteRepositoryAction(() => _enumRepository.DeleteEnum(enumNameValue),
                $"Delete enum name value failed for enum name value:'{enumNameValue}'");
        }

        public async Task<IActionResult> DeleteEnums(params string[] enumNameValues)
        {
            return await ExecuteRepositoryAction(() => _enumRepository.DeleteEnums(enumNameValues),
                $"Delete enum name values failed for enum name values:'{enumNameValues.AsJson()}'");
        }

        public async Task<IActionResult> DeleteCalculation(string calculationId)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.DeleteCalculation(calculationId),
                $"Delete calculation failed for calculation:'{calculationId}'");
        }
        
        public async Task<IActionResult> DeleteCalculations(params string[] calculationIds)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.DeleteCalculations(calculationIds),
                $"Delete calculation failed for calculation:'{calculationIds.AsJson()}'");
        }

        public async Task<IActionResult> UpsertCalculations(IEnumerable<Calculation> calculations)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.UpsertCalculations(calculations),
                $"Upsert calculations failed for calculations:'{calculations.AsJson()}'");
        }

        public async Task<IActionResult> UpsertCalculationSpecificationRelationship(string calculationId, string specificationId)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.UpsertCalculationSpecificationRelationship(calculationId, specificationId),
                $"Upsert calculation relationship between specification failed for calculation:'{calculationId}'" +
                $" and specification:'{specificationId}'");
        }
        
        public async Task<IActionResult> UpsertCalculationSpecificationRelationships(params (string calculationId, string specificationId)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.UpsertCalculationSpecificationRelationships(relationships),
                $"Upsert calculation relationships calculation -> specification");
        }

        public async Task<IActionResult> UpsertCalculationEnumRelationship(string calculationId, string enumId)
        {
            return await ExecuteRepositoryAction(() => _enumRepository.UpsertCalculationEnumRelationship(calculationId, enumId),
                $"Upsert calculation relationship between enum failed for calculation:'{calculationId}'" +
                $" and enum:'{enumId}'");
        }

        public async Task<IActionResult> UpsertCalculationEnumRelationships(params (string calculationId, string enumId)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _enumRepository.UpsertCalculationEnumRelationships(relationships),
                $"Upsert calculation relationships calculation -> enum");
        }

        public async Task<IActionResult> GetCalculationCircularDependencies(string specificationId)
        {
            string cacheKey = $"{CacheKeys.CircularDependencies}{specificationId}";

            IEnumerable<Entity<Calculation, Relationship>> circularDependencies = await _cachePolicy.ExecuteAsync(() => _cacheProvider.GetAsync<List<Entity<Calculation, Relationship>>>(cacheKey));
            
            if (circularDependencies != null)
            {
                return new OkObjectResult(circularDependencies);
            }

            IEnumerable<Entity<Calculation, IRelationship>> calculations = await _calcRepository.GetAllCalculationsForSpecification(specificationId);

            PagedContext<string> pagedRequests = new PagedContext<string>(calculations
                    .Select(_ => _.Node.CalculationId),
                PageSize);

            circularDependencies = new Entity<Calculation, Relationship>[0];

            if (calculations.AnyWithNullCheck())
            {
                return await ExecuteRepositoryAction(async () =>
                {
                    
                    while (pagedRequests.HasPages)
                    {
                        circularDependencies = circularDependencies.Concat(await _calcRepository.GetCalculationCircularDependencies(pagedRequests
                        .NextPage()
                        .ToArray()));
                    }

                    await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, circularDependencies.DistinctBy(_ => _.Node.CalculationId).ToList(), TimeSpan.FromDays(7), true));
                        
                    return circularDependencies;
                }, $"Unable to retrieve calculation circular dependencies for specification {specificationId}.");
            }
            else
            {
                return new OkObjectResult(circularDependencies);
            }
        }

        public async Task<IActionResult> GetAllCalculationsForAll(params string[] calculationIds)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.GetAllEntitiesForAll(calculationIds), 
                $"Unable to retrieve all entities for calc ids {calculationIds.AsJson()}");
        }

        public async Task<IActionResult> GetAllFundingLinesForAll(params string[] fundingLineIds)
        {
            return await ExecuteRepositoryAction(() => _fundingLineRepository.GetAllEntitiesForAll(fundingLineIds), 
                $"Unable to retrieve all entities for funding line ids {fundingLineIds.AsJson()}");
        }
        
        public async Task<IActionResult> GetAllEntities<TNode>(string nodeId)
            where TNode : class
        {
            if(typeof(TNode).IsAssignableFrom(typeof(Specification)))
            {
                return await ExecuteRepositoryAction(() => _specRepository.GetAllEntities(nodeId), $"Unable to retrieve all entities for {typeof(TNode).Name.ToLowerInvariant()}.");
            }
            else if(typeof(TNode).IsAssignableFrom(typeof(Calculation)))
            {
                return await ExecuteRepositoryAction(() => _calcRepository.GetAllEntities(nodeId), $"Unable to retrieve all entities for {typeof(TNode).Name.ToLowerInvariant()}.");
            }
            else if (typeof(TNode).IsAssignableFrom(typeof(DataField)))
            {
                return await ExecuteRepositoryAction(() => _datasetRepository.GetAllEntities(nodeId), $"Unable to retrieve all entities for {typeof(TNode).Name.ToLowerInvariant()}.");
            }
            else if (typeof(TNode).IsAssignableFrom(typeof(FundingLine)))
            {
                return await ExecuteRepositoryAction(() => _fundingLineRepository.GetAllEntities(nodeId), $"Unable to retrieve all entities for {typeof(TNode).Name.ToLowerInvariant()}.");
            }
            else if (typeof(TNode).IsAssignableFrom(typeof(Enum)))
            {
                return await ExecuteRepositoryAction(() => _enumRepository.GetAllEntities(nodeId), $"Unable to retrieve all entities for {typeof(TNode).Name.ToLowerInvariant()}.");
            }
            else if (typeof(TNode).IsAssignableFrom(typeof(DatasetRelationship)))
            {
                return await ExecuteRepositoryAction(() => _datasetRepository.GetAllDatasetRelationsshipEntities(nodeId), $"Unable to retrieve all entities for {typeof(TNode).Name.ToLowerInvariant()}.");
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public async Task<IActionResult> UpsertCalculationCalculationRelationship(string calculationIdA, string calculationIdB)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.UpsertCalculationCalculationRelationship(calculationIdA, calculationIdB),
                $"Upsert calculation relationship call to calculation failed for calculation:'{calculationIdA}'" +
                $" calling calculation:'{calculationIdB}'");
        }

        public async Task<IActionResult> UpsertCalculationCalculationsRelationships(string calculationId, string[] calculationIds)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.UpsertCalculationCalculationRelationships(calculationIds.Select(_ => (calculationId, _)).ToArray()),
                $"Upsert calculation relationship call to calculation failed for calculation:'{calculationId}'" +
                $" calling calculations:'{calculationIds.AsJson()}'");
        }

        public async Task<IActionResult> DeleteCalculationSpecificationRelationship(string calculationId, string specificationId)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.DeleteCalculationSpecificationRelationship(calculationId, specificationId),
                $"Delete calculation relationship between specification failed for calculation:'{calculationId}'" +
                $" and specification:'{specificationId}'");
        }
        
        public async Task<IActionResult> DeleteCalculationSpecificationRelationships(params (string calculationId, string specificationId)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.DeleteCalculationSpecificationRelationships(relationships),
                $"Delete calculations between specification failed ");
        }

        public async Task<IActionResult> DeleteCalculationCalculationRelationship(string calculationIdA, string calculationIdB)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.DeleteCalculationCalculationRelationship(calculationIdA, calculationIdB),
                $"Delete calculation relationship call to calculation failed for calculation:'{calculationIdA}'" +
                $" calling calculation:'{calculationIdB}'");
        }
        
        public async Task<IActionResult> DeleteCalculationCalculationRelationships(params (string calculationIdA, string calculationIdB)[] relationships)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.DeleteCalculationCalculationRelationships(relationships),
                $"Delete calculation relationships call to calculation failed");
        }

        public async Task<IActionResult> UpsertCalculationDataFieldsRelationships(string calculationId, string[] datasetFieldIds)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.UpsertCalculationDataFieldRelationships(datasetFieldIds.Select(_ => (calculationId, _)).ToArray()),
                $"Upsert datafield relationship call to calculation failed for calculation:'{calculationId}'" +
                $" calling calculations:'{datasetFieldIds.AsJson()}'");
        }

        public async Task<IActionResult> DeleteCalculationDatasetFieldRelationship(string calculationId, string datasetFieldId)
        {
            return await ExecuteRepositoryAction(() => _calcRepository.DeleteCalculationDataFieldRelationship(calculationId, datasetFieldId),
                $"Delete calculation relationship call to datasetfield failed for calculation:'{calculationId}'" +
                $" calling calculation:'{datasetFieldId}'");
        }

        public async Task<IActionResult> DeleteDatasetField(string datasetFieldId)
        {
            return await ExecuteRepositoryAction(() => _datasetRepository.DeleteDataField(datasetFieldId),
                $"Unable to delete datasetField {datasetFieldId}");
        }

        private async Task<IActionResult> ExecuteRepositoryAction<T>(Func<Task<T>> action, string errorMessage)
        {
            try
            {
                T results =  await action();

                return new OkObjectResult(results);
            }
            catch (Exception e)
            {
                _logger.Error(e, errorMessage);

                throw;
            }
        }

        private async Task<IActionResult> ExecuteRepositoryAction(Func<Task> action, string errorMessage)
        {
            try
            {
                await action();

                return new OkResult();
            }
            catch (Exception e)
            {
                _logger.Error(e, errorMessage);

                throw;
            }
        }
    }
}
