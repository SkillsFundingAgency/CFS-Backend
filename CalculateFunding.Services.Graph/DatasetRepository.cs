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

        public async Task UpsertDatasetRelationship(DatasetRelationship datasetRelationship)
        {
            await UpsertNode(datasetRelationship, AttributeConstants.DatasetRelationshipId);
        }

        public async Task UpsertDatasetRelationships(DatasetRelationship[] datasetRelationships)
        {
            await UpsertNodes(datasetRelationships, AttributeConstants.DatasetRelationshipId);
        }

        public async Task DeleteDatasetRelationship(string relationshipId)
        {
            await DeleteNode<DatasetRelationship>(AttributeConstants.DatasetRelationshipId, relationshipId);
        }

        public async Task DeleteDatasetRelationships(string[] relationshipIds)
        {
            await DeleteNodes<DatasetRelationship>(relationshipIds.Select(_ => (AttributeConstants.DatasetRelationshipId, _)).ToArray());
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

        public async Task UpsertDatasetRelationshipDataFieldRelationships(params (string datasetRelationshipId, string dataFieldUniqueId)[] relationships)
        {
            await UpsertRelationships<DatasetRelationship, DataField>(relationships.Select(_ => (AttributeConstants.DatasetRelationshipDataFieldRelationshipId,
                (AttributeConstants.DatasetRelationshipId, _.datasetRelationshipId),
                (AttributeConstants.UniqueDataFieldId, _.dataFieldUniqueId)))
                .ToArray());

            await UpsertRelationships<DataField, DatasetRelationship>(relationships.Select(_ => (AttributeConstants.DataFieldDatasetRelationshipRelationshipId,
                (AttributeConstants.UniqueDataFieldId, _.dataFieldUniqueId),
                (AttributeConstants.DatasetRelationshipId, _.datasetRelationshipId)))
                .ToArray());
        }

        public async Task DeleteDatasetRelationshipDataFieldRelationship(string datasetRelationshipId, string dataFieldUniqueId)
        {
            await DeleteRelationship<DatasetRelationship, DataField>(AttributeConstants.DatasetRelationshipDataFieldRelationshipId,
                (AttributeConstants.DatasetRelationshipId, datasetRelationshipId),
                (AttributeConstants.UniqueDataFieldId, dataFieldUniqueId));

            await DeleteRelationship<DataField, DatasetRelationship>(AttributeConstants.DataFieldDatasetRelationshipRelationshipId,
                (AttributeConstants.UniqueDataFieldId, dataFieldUniqueId),
                (AttributeConstants.DatasetRelationshipId, datasetRelationshipId));
        }

        public async Task DeleteDatasetRelationshipDataFieldRelationships((string datasetRelationshipId, string dataFieldUniqueId)[] relationships)
        {
            await DeleteRelationships<DatasetRelationship, DataField>(relationships.Select(_ => (AttributeConstants.DatasetRelationshipDataFieldRelationshipId,
               (AttributeConstants.DatasetRelationshipId, _.datasetRelationshipId),
               (AttributeConstants.UniqueDataFieldId, _.dataFieldUniqueId)))
                .ToArray());

            await DeleteRelationships<DataField, DatasetRelationship>(relationships.Select(_ => (AttributeConstants.DataFieldDatasetRelationshipRelationshipId,
                (AttributeConstants.UniqueDataFieldId, _.dataFieldUniqueId),
                (AttributeConstants.DatasetRelationshipId, _.datasetRelationshipId)))
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
                new[] 
                { 
                    AttributeConstants.DataFieldCalculationRelationship,
                    AttributeConstants.CalculationDataFieldRelationshipId,
                });
            return entities.Select(_ => new Entity<DataField, IRelationship> { Node = _.Node, Relationships = _.Relationships });
        }

        public async Task<IEnumerable<Entity<DatasetRelationship, IRelationship>>> GetAllDatasetRelationsshipEntitiesForAll(params string[] relationshipIds)
        {
            IEnumerable<Entity<DatasetRelationship>> entities = await GetAllEntitiesForAll<DatasetRelationship>(AttributeConstants.DatasetRelationshipId,
                relationshipIds,
                new[] 
                {
                    AttributeConstants.DatasetRelationshipDataFieldRelationshipId,
                    AttributeConstants.DataFieldDatasetRelationshipRelationshipId
                });
            return entities.Select(_ => new Entity<DatasetRelationship, IRelationship> { Node = _.Node, Relationships = _.Relationships });
        }

        public async Task<IEnumerable<Entity<DatasetRelationship, IRelationship>>> GetAllDatasetRelationsshipEntities(string relationshipId)
        {
            IEnumerable<Entity<DatasetRelationship>> entities = await GetAllEntities<DatasetRelationship>(AttributeConstants.DatasetRelationshipId,
                relationshipId,
                new[]
                {
                    AttributeConstants.DatasetRelationshipDataFieldRelationshipId,
                    AttributeConstants.DataFieldDatasetRelationshipRelationshipId
                });
            return entities.Select(_ => new Entity<DatasetRelationship, IRelationship>
            {
                Node = _.Node,
                Relationships = _.Relationships
            });
        }
    }
}