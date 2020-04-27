using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Models.Graph;

namespace CalculateFunding.Services.Graph.Interfaces
{
    public interface IDatasetRepository
    {
        Task UpsertDataset(Dataset dataset);
        Task UpsertDatasets(Dataset[] datasets);
        Task DeleteDataset(string datasetId);
        Task UpsertDatasetDefinition(DatasetDefinition datasetDefinition);
        Task UpsertDatasetDefinitions(DatasetDefinition[] datasetDefinition);
        Task DeleteDatasetDefinition(string datasetDefinitionId);
        Task UpsertDataField(DataField dataField);
        Task DeleteDataField(string dataFieldId);
        Task UpsertDataFields(IEnumerable<DataField> datasetField);
        Task UpsertDataDefinitionDatasetRelationship(string datasetDefinitionId, string datasetId);
        Task UpsertDatasetDataFieldRelationship(string datasetId, string dataFieldId);
        Task DeleteDataDefinitionDatasetRelationship(string datasetDefinitionId, string datasetId);
        Task DeleteDatasetDataFieldRelationship(string datasetId, string dataFieldId);
        Task<IEnumerable<Entity<DataField, IRelationship>>> GetAllEntities(string datasetFieldId);
    }
}