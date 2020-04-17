using System.Collections.Generic;
using System.Threading.Tasks;
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
        private const string DatasetFieldId = DatasetField.IdField;
        public const string CalculationId = "calculationid";

        public const string DatasetDatasetDefinitionRelationship = "IsForSchema";
        public const string DatasetDefinitionDatasetRelationship = "HasDataset";
        public const string DataFieldDatasetRelationship = "IsInDataset";
        public const string DatasetDataFieldRelationship = "HasDataField";
        public const string CalculationDatasetFieldRelationship = "ReferencesDatasetField";
        public const string DatasetFieldCalculationRelationship = "IsReferencedInCalculation";


        public DatasetRepository(IGraphRepository graphRepository) 
            : base(graphRepository)
        {
        }

        public async Task UpsertDataset(Dataset dataset)
        {
            await UpsertNode(dataset, DatasetId);
        }
        
        public async Task DeleteDataset(string datasetId)
        {
            await DeleteNode<Dataset>(DatasetId, datasetId);
        }
        
        public async Task UpsertDatasetDefinition(DatasetDefinition datasetDefinition)
        {
            await UpsertNode(datasetDefinition, DatasetDefinitionId);
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

        public async Task UpsertDatasetField(IEnumerable<DatasetField> datasetFields) 
        {
            await UpsertNodes(datasetFields, DatasetFieldId);
        }
                
        public async Task CreateDataDefinitionDatasetRelationship(string datasetDefinitionId, string datasetId)
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

        public async Task CreateDatasetDataFieldRelationship(string datasetId, string dataFieldId)
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

        public async Task UpsertCalculationDatasetFieldRelationship(string calculationId, string datasetFieldId)
        {
            await UpsertRelationship<Calculation, DatasetField>(CalculationDatasetFieldRelationship,
                (CalculationId, calculationId),
                (DatasetFieldId, datasetFieldId));

            await UpsertRelationship<DatasetField, Calculation>(DatasetFieldCalculationRelationship,
                (DatasetFieldId, datasetFieldId),
                (CalculationId, calculationId));
        }

        public async Task DeleteCalculationDatasetFieldRelationship(string calculationId,
            string datasetFieldId)
        {
            await DeleteRelationship<Calculation, DatasetField>(CalculationDatasetFieldRelationship,
                (CalculationId, calculationId),
                (DatasetFieldId, datasetFieldId));

            await DeleteRelationship<DatasetField, Calculation>(DatasetFieldCalculationRelationship,
                (DatasetFieldId, datasetFieldId),
                (CalculationId, calculationId));
        }

        public async Task DeleteDatasetField(string datasetFieldId)
        {
            await DeleteNode<DatasetField>(DatasetFieldId, datasetFieldId);
        }
    }
}