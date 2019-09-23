using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;

namespace CalculateFunding.Services.Scenarios.Interfaces
{
    [Obsolete("Replace with common nuget API client")]
    public interface IDatasetRepository
    {
        Task<IEnumerable<DatasetSchemaRelationshipModel>> GetDatasetSchemaRelationshipModelsForSpecificationId(string specificationId);

        Task<IEnumerable<DatasetSpecificationRelationshipViewModel>> GetCurrentRelationshipsBySpecificationId(string specificationId);

        Task<DatasetDefinition> GetDatasetDefinitionById(string datasetDefinitionId);

        Task<IEnumerable<string>> GetRelationshipSpecificationIdsByDatasetDefinitionId(string datasetDefinitionId);

        Task<IEnumerable<DatasetSpecificationRelationshipViewModel>> GetCurrentRelationshipsBySpecificationIdAndDatasetDefinitionId(string specificationId, string datasetDefinitionId);
    }
}
