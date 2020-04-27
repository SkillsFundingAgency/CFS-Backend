using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;

namespace CalculateFunding.Services.Graph
{
    public class DatasetRepository : GraphRepositoryBase, IDatasetRepository
    {
        private const string DatasetId = Dataset.IdField;
        private const string DatasetDefinitionId = DatasetDefinition.IdField;
        private const string DataFieldId = DataField.IdField;
        public const string CalculationId = "calculationid";

        public const string DatasetDatasetDefinitionRelationship = "IsForSchema";
        public const string DatasetDefinitionDatasetRelationship = "HasDataset";
        public const string DataFieldDatasetRelationship = "IsInDataset";
        public const string DatasetDataFieldRelationship = "HasDatasetField";


        public DatasetRepository(IGraphRepository graphRepository) 
            : base(graphRepository)
        {
        }

        public async Task UpsertDataset(Dataset dataset)
        {
            await UpsertNode(dataset, DatasetId);
        }

        public async Task UpsertDatasets(Dataset[] datasets)
        {
            await UpsertNodes(datasets, DatasetId);
        }

        public async Task DeleteDataset(string datasetId)
        {
            await DeleteNode<Dataset>(DatasetId, datasetId);
        }
        
        public async Task UpsertDatasetDefinition(DatasetDefinition datasetDefinition)
        {
            await UpsertNode(datasetDefinition, DatasetDefinitionId);
        }
        public async Task UpsertDatasetDefinitions(DatasetDefinition[] datasetDefinitions)
        {
            await UpsertNodes(datasetDefinitions, DatasetDefinitionId);
        }

        public async Task DeleteDatasetDefinition(string datasetDefinitionId)
        {
            await DeleteNode<DatasetDefinition>(DatasetDefinitionId, datasetDefinitionId);
        }
        
        public async Task UpsertDataField(DataField dataField)
        {
            await UpsertNode(dataField, DataFieldId);
        }
        
        public async Task DeleteDataField(string dataFieldId)
        {
            await DeleteNode<DataField>(DataFieldId, dataFieldId);
        }

        public async Task UpsertDataFields(IEnumerable<DataField> datasetFields) 
        {
            await UpsertNodes(datasetFields, DataFieldId);
        }
                
        public async Task UpsertDataDefinitionDatasetRelationship(string datasetDefinitionId, string datasetId)
        {
            await UpsertRelationship<DatasetDefinition, Dataset>(DatasetDefinitionDatasetRelationship,
                (DatasetDefinitionId, datasetDefinitionId),
                (DatasetId, datasetId));
            
            await UpsertRelationship<Dataset, DatasetDefinition>(DatasetDatasetDefinitionRelationship,
                (DatasetId, datasetId),
                (DatasetDefinitionId, datasetDefinitionId));
        }
        
        public async Task DeleteDataDefinitionDatasetRelationship(string datasetDefinitionId, string datasetId)
        {
            await DeleteRelationship<DatasetDefinition, Dataset>(DatasetDefinitionDatasetRelationship,
                (DatasetDefinitionId, datasetDefinitionId),
                (DatasetId, datasetId));
            
            await DeleteRelationship<Dataset, DatasetDefinition>(DatasetDatasetDefinitionRelationship,
                (DatasetId, datasetId),
                (DatasetDefinitionId, datasetDefinitionId));
        }

        public async Task UpsertDatasetDataFieldRelationship(string datasetId, string dataFieldId)
        {
            await UpsertRelationship<Dataset, DataField>(DatasetDataFieldRelationship,
                (DatasetId, datasetId),
                (DataFieldId, dataFieldId));
            
            await UpsertRelationship<DataField, Dataset>(DataFieldDatasetRelationship,
                (DataFieldId, dataFieldId),
                (DatasetId, datasetId));
        }
        
        public async Task DeleteDatasetDataFieldRelationship(string datasetId, string dataFieldId)
        {
            await DeleteRelationship<Dataset, DataField>(DatasetDataFieldRelationship,
                (DatasetId, datasetId),
                (DataFieldId, dataFieldId));
            
            await DeleteRelationship<DataField, Dataset>(DataFieldDatasetRelationship,
                (DataFieldId, dataFieldId),
                (DatasetId, datasetId));
        }

        public async Task<IEnumerable<Entity<DataField, IRelationship>>> GetAllEntities(string datasetFieldId)
        {
            IEnumerable<Entity<DataField>> entities = await GetAllEntities<DataField>(DataFieldId,
                datasetFieldId,
                new[] { CalculationRepository.DataFieldCalculationRelationship,
                    CalculationRepository.CalculationDataFieldRelationship
                });
            return entities.Select(_ => new Entity<DataField, IRelationship> { Node = _.Node, Relationships = _.Relationships });
        }
    }
}