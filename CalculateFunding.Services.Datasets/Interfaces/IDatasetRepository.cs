using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;

namespace CalculateFunding.Services.Datasets.Interfaces
{
    public interface IDatasetRepository
    {
        Task<HttpStatusCode> SaveDefinition(DatasetDefinition definition);

        Task<IEnumerable<DatasetDefinition>> GetDatasetDefinitions();

        Task<IEnumerable<Dataset>> GetDatasetsByQuery(Expression<Func<DocumentEntity<Dataset>, bool>> query);

        Task<IEnumerable<DatasetDefinition>> GetDatasetDefinitionsByQuery(Expression<Func<DocumentEntity<DatasetDefinition>, bool>> query);

        Task<HttpStatusCode> SaveDataset(Dataset dataset);

        Task SaveDatasets(IEnumerable<Dataset> datasets);

        Task<HttpStatusCode> SaveDefinitionSpecificationRelationship(DefinitionSpecificationRelationship relationship);

        Task<DatasetDefinition> GetDatasetDefinition(string definitionId);

        Task<IEnumerable<DefinitionSpecificationRelationship>> GetDefinitionSpecificationRelationshipsByQuery(Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>> query);

        Task<DefinitionSpecificationRelationship> GetRelationshipBySpecificationIdAndName(string specificationId, string name);

        Task<IEnumerable<DefinitionSpecificationRelationship>> GetAllDefinitionSpecificationsRelationships();

        Task<Dataset> GetDatasetByDatasetId(string datasetId);

        Task<DocumentEntity<Dataset>> GetDatasetDocumentByDatasetId(string datasetId);

        Task<IEnumerable<DocumentEntity<Dataset>>> GetDatasets();

        Task<DefinitionSpecificationRelationship> GetDefinitionSpecificationRelationshipById(string relationshipId);

        Task<HttpStatusCode> UpdateDefinitionSpecificationRelationship(DefinitionSpecificationRelationship relationship);

        Task<IEnumerable<string>> GetDistinctRelationshipSpecificationIdsForDatasetDefinitionId(string datasetDefinitionId);

        Task UpdateDefinitionSpecificationRelationships(IEnumerable<DefinitionSpecificationRelationship> relationships);
    }
}
