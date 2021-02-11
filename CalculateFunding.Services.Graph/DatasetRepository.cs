using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Constants;
using CalculateFunding.Services.Graph.Interfaces;

namespace CalculateFunding.Services.Graph
{
    public class DatasetRepository : GraphRepositoryBase, IDatasetRepository
    {
        public DatasetRepository(IGraphRepository graphRepository) 
            : base(graphRepository)
        {
        }

        public async Task UpsertDataset(Dataset dataset)
        {
            await UpsertNode(dataset, AttributeConstants.DatasetId);
        }

        public async Task UpsertDatasets(Dataset[] datasets)
        {
            await UpsertNodes(datasets, AttributeConstants.DatasetId);
        }

        public async Task DeleteDataset(string datasetId)
        {
            await DeleteNode<Dataset>(AttributeConstants.DatasetId, datasetId);
        }
        
        public async Task UpsertDatasetDefinition(DatasetDefinition datasetDefinition)
        {
            await UpsertNode(datasetDefinition, AttributeConstants.DatasetDefinitionId);
        }
        public async Task UpsertDatasetDefinitions(DatasetDefinition[] datasetDefinitions)
        {
            await UpsertNodes(datasetDefinitions, AttributeConstants.DatasetDefinitionId);
        }

        public async Task DeleteDatasetDefinition(string datasetDefinitionId)
        {
            await DeleteNode<DatasetDefinition>(AttributeConstants.DatasetDefinitionId, datasetDefinitionId);
        }
        
        public async Task UpsertDataField(DataField dataField)
        {
            await UpsertNode(dataField, AttributeConstants.DataFieldId);
        }
        
        public async Task DeleteDataField(string dataFieldId)
        {
            await DeleteNode<DataField>(AttributeConstants.DataFieldId, dataFieldId);
        }

        public async Task UpsertDataFields(IEnumerable<DataField> datasetFields) 
        {
            await UpsertNodes(datasetFields, AttributeConstants.DataFieldId);
        }
                
        public async Task UpsertDataDefinitionDatasetRelationship(string datasetDefinitionId, string datasetId)
        {
            await UpsertRelationship<DatasetDefinition, Dataset>(AttributeConstants.DatasetDefinitionDatasetRelationshipId,
                (AttributeConstants.DatasetDefinitionId, datasetDefinitionId),
                (AttributeConstants.DatasetId, datasetId));
            
            await UpsertRelationship<Dataset, DatasetDefinition>(AttributeConstants.DatasetDatasetDefinitionRelationshipId,
                (AttributeConstants.DatasetId, datasetId),
                (AttributeConstants.DatasetDefinitionId, datasetDefinitionId));
        }
        
        public async Task UpsertDataDefinitionDatasetRelationships(params (string datasetDefinitionId, string datasetId)[] relationships)
        {
            await UpsertRelationships<DatasetDefinition, Dataset>(relationships.Select(_ => (AttributeConstants.DatasetDefinitionDatasetRelationshipId,
                (AttributeConstants.DatasetDefinitionId, _.datasetDefinitionId),
                (AttributeConstants.DatasetId, _.datasetId)))
                .ToArray());
            
            await UpsertRelationships<Dataset, DatasetDefinition>(relationships.Select(_ => (AttributeConstants.DatasetDatasetDefinitionRelationshipId,
                (AttributeConstants.DatasetId, _.datasetId),
                (AttributeConstants.DatasetDefinitionId, _.datasetDefinitionId)))
                .ToArray());
        }
        
        public async Task DeleteDataDefinitionDatasetRelationship(string datasetDefinitionId, string datasetId)
        {
            await DeleteRelationship<DatasetDefinition, Dataset>(AttributeConstants.DatasetDefinitionDatasetRelationshipId,
                (AttributeConstants.DatasetDefinitionId, datasetDefinitionId),
                (AttributeConstants.DatasetId, datasetId));
            
            await DeleteRelationship<Dataset, DatasetDefinition>(AttributeConstants.DatasetDatasetDefinitionRelationshipId,
                (AttributeConstants.DatasetId, datasetId),
                (AttributeConstants.DatasetDefinitionId, datasetDefinitionId));
        }

        public async Task UpsertDatasetDataFieldRelationship(string datasetId, string dataFieldId)
        {
            await UpsertRelationship<Dataset, DataField>(AttributeConstants.DatasetDataFieldRelationshipId,
                (AttributeConstants.DatasetId, datasetId),
                (AttributeConstants.DataFieldId, dataFieldId));
            
            await UpsertRelationship<DataField, Dataset>(AttributeConstants.DataFieldDatasetRelationshipId,
                (AttributeConstants.DataFieldId, dataFieldId),
                (AttributeConstants.DatasetId, datasetId));
        }
        
        public async Task UpsertDatasetDataFieldRelationships(params (string datasetId, string dataFieldId)[] relationships)
        {
            await UpsertRelationships<Dataset, DataField>(relationships.Select(_ => (AttributeConstants.DatasetDataFieldRelationshipId,
                (AttributeConstants.DatasetId, _.datasetId),
                (AttributeConstants.DataFieldId, _.dataFieldId)))
                .ToArray());
            
            await UpsertRelationships<DataField, Dataset>(relationships.Select(_ => (AttributeConstants.DataFieldDatasetRelationshipId,
                (AttributeConstants.DataFieldId, _.dataFieldId),
                (AttributeConstants.DatasetId, _.datasetId)))
                .ToArray());
        }
        
        public async Task DeleteDatasetDataFieldRelationship(string datasetId, string dataFieldId)
        {
            await DeleteRelationship<Dataset, DataField>(AttributeConstants.DatasetDataFieldRelationshipId,
                (AttributeConstants.DatasetId, datasetId),
                (AttributeConstants.DataFieldId, dataFieldId));
            
            await DeleteRelationship<DataField, Dataset>(AttributeConstants.DataFieldDatasetRelationshipId,
                (AttributeConstants.DataFieldId, dataFieldId),
                (AttributeConstants.DatasetId, datasetId));
        }

        public async Task<IEnumerable<Entity<DataField, IRelationship>>> GetAllEntities(string datasetFieldId)
        {
            IEnumerable<Entity<DataField>> entities = await GetAllEntities<DataField>(AttributeConstants.DataFieldId,
                datasetFieldId,
                new[] { AttributeConstants.DataFieldCalculationRelationship,
                    AttributeConstants.CalculationDataFieldRelationshipId
                });
            return entities.Select(_ => new Entity<DataField, IRelationship> { Node = _.Node, Relationships = _.Relationships });
        }
    }
}