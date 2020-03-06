using System.Threading.Tasks;
using CalculateFunding.Models.Graph;

namespace CalculateFunding.Services.Graph.Interfaces
{
    public interface IDatasetRepository
    {
        Task UpsertDataset(Dataset dataset);
        Task DeleteDataset(string datasetId);
        Task UpsertDatasetDefinition(DatasetDefinition datasetDefinition);
        Task DeleteDatasetDefinition(string datasetDefinitionId);
        Task UpsertDataField(DataField dataField);
        Task DeleteDataField(string dataFieldId);
        Task CreateDataDefinitionDatasetRelationship(string datasetDefinitionId, string datasetId);
        Task CreateDatasetDataFieldRelationship(string datasetId, string dataFieldId);
        Task DeleteDataDefinitionDatasetRelationship(string datasetDefinitionId, string datasetId);
        Task DeleteDatasetDataFieldRelationship(string datasetId, string dataFieldId);
    }
}