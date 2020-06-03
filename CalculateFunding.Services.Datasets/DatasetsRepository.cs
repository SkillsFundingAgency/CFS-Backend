using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Messages;
using CalculateFunding.Services.Datasets.Interfaces;

namespace CalculateFunding.Services.Datasets
{
    public class DataSetsRepository : IDatasetRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;

        public DataSetsRepository(CosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));

            _cosmosRepository = cosmosRepository;
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            (bool Ok, string Message) = _cosmosRepository.IsHealthOk();

            health.Name = nameof(DataSetsRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = this.GetType().Name, Message = Message });

            return Task.FromResult(health);
        }

        public Task<HttpStatusCode> SaveDefinition(DatasetDefinition definition)
        {
            return _cosmosRepository.UpsertAsync(definition);
        }

        public Task<HttpStatusCode> SaveDataset(Dataset dataset)
        {
            return _cosmosRepository.UpsertAsync(dataset);
        }

        public async Task SaveDatasets(IEnumerable<Dataset> datasets)
        {
            await _cosmosRepository.BulkUpsertAsync(datasets.ToList());
        }

        public async Task<DatasetDefinition> GetDatasetDefinition(string definitionId)
        {
            IEnumerable<DatasetDefinition> definitions = await GetDatasetDefinitionsByQuery(m => m.Id == definitionId);

            return definitions.FirstOrDefault();
        }

        public async Task<Dataset> GetDatasetByDatasetId(string datasetId)
        {
            IEnumerable<Dataset> datasets = await GetDatasetsByQuery(m => m.Id == datasetId);

            return datasets.FirstOrDefault();
        }

        public async Task<IEnumerable<DatasetDefinition>> GetDatasetDefinitions()
        {
            return await _cosmosRepository.Query<DatasetDefinition>();
        }

        public async Task<IEnumerable<DatasetDefinition>> GetDatasetDefinitionsByQuery(Expression<Func<DocumentEntity<DatasetDefinition>, bool>> query)
        {
            return await _cosmosRepository.Query<DatasetDefinition>(query);
        }

        public async Task<IEnumerable<Dataset>> GetDatasetsByQuery(Expression<Func<DocumentEntity<Dataset>, bool>> query)
        {
            return await _cosmosRepository.Query<Dataset>(query);
        }

        public Task<HttpStatusCode> SaveDefinitionSpecificationRelationship(DefinitionSpecificationRelationship relationship)
        {
            return _cosmosRepository.CreateAsync(relationship);
        }

        public Task<HttpStatusCode> UpdateDefinitionSpecificationRelationship(DefinitionSpecificationRelationship relationship)
        {
            return _cosmosRepository.UpdateAsync(relationship);
        }

        public async Task UpdateDefinitionSpecificationRelationships(IEnumerable<DefinitionSpecificationRelationship> relationships)
        {
            await _cosmosRepository.BulkUpsertAsync(relationships.ToList());
        }

        public async Task<IEnumerable<DefinitionSpecificationRelationship>> GetDefinitionSpecificationRelationshipsByQuery(Expression<Func<DocumentEntity<DefinitionSpecificationRelationship>, bool>> query)
        {
            return await _cosmosRepository.Query<DefinitionSpecificationRelationship>(query);
        }

        public async Task<DefinitionSpecificationRelationship> GetRelationshipBySpecificationIdAndName(string specificationId, string name)
        {
            IEnumerable<DefinitionSpecificationRelationship> relationships = await GetDefinitionSpecificationRelationshipsByQuery(m => m.Content.Specification.Id == specificationId);

            if (relationships.IsNullOrEmpty())
            {
                return null;
            }

            return relationships.FirstOrDefault(m => string.Equals(m.Name.RemoveAllSpaces(), name.RemoveAllSpaces(), StringComparison.InvariantCultureIgnoreCase));
        }

        public async Task<DefinitionSpecificationRelationship> GetDefinitionSpecificationRelationshipById(string relationshipId)
        {
            IEnumerable<DefinitionSpecificationRelationship> relationships = await GetDefinitionSpecificationRelationshipsByQuery(m => m.Id == relationshipId);

            return relationships.FirstOrDefault();
        }

        public Task<IEnumerable<DocumentEntity<Dataset>>> GetDatasets()
        {
            return _cosmosRepository.GetAllDocumentsAsync<Dataset>();
        }

        public async Task<DocumentEntity<Dataset>> GetDatasetDocumentByDatasetId(string datasetId)
        {
            return (await _cosmosRepository.QueryDocuments<Dataset>(1)).Where(c => c.Id == datasetId && !c.Deleted).FirstOrDefault();
        }

        public async Task<IEnumerable<DefinitionSpecificationRelationship>> GetAllDefinitionSpecificationsRelationships()
        {
            return await _cosmosRepository.Query<DefinitionSpecificationRelationship>();
        }

        public async Task<IEnumerable<string>> GetDistinctRelationshipSpecificationIdsForDatasetDefinitionId(string datasetDefinitionId)
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT d.content.Specification.id AS specificationId
                            FROM    datasets d
                            WHERE   d.deleted = false 
                                    AND d.documentType = ""DefinitionSpecificationRelationship"" 
                                    AND d.content.DatasetDefinition.id = @DatasetDefinitionId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@DatasetDefinitionID", datasetDefinitionId)
                }
            };

            HashSet<string> specificationIds = new HashSet<string>();

            IEnumerable<dynamic> results = await _cosmosRepository.DynamicQuery(cosmosDbQuery, 1000);

            foreach (dynamic result in results)
            {
                specificationIds.Add(result.specificationId);
            }

            return specificationIds;
        }

        public async Task<IEnumerable<KeyValuePair<string, int>>> GetDatasetLatestVersions(IEnumerable<string> datasetIds)
        {
            Guard.IsNotEmpty(datasetIds, nameof(datasetIds));

            StringBuilder queryTextBuilder = new StringBuilder(@"
                SELECT d.id, d.content.current.version
                FROM datasets d
                WHERE d.deleted = false 
                AND d.documentType = 'Dataset'");

            string documentIdQueryText = string.Join(',', datasetIds.Select((_, index) => $"@datasetId_{index}"));
            queryTextBuilder.Append($" AND d.id IN ({documentIdQueryText})");

            IEnumerable<CosmosDbQueryParameter> cosmosDbQueryParameters = datasetIds.Select((_, index) => new CosmosDbQueryParameter($"@datasetId_{index}", _));

            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = queryTextBuilder.ToString(),
                Parameters = cosmosDbQueryParameters
            };

            IDictionary<string, int> results = new Dictionary<string, int>();
            IEnumerable<dynamic> queryResults = await _cosmosRepository
             .DynamicQuery(cosmosDbQuery);

            foreach (dynamic item in queryResults)
            {
                results.Add((string)item.id, (int)item.version);
            }

            return await Task.FromResult(results);
        }

        public async Task DeleteDatasetsBySpecificationId(string specificationId, DeletionType deletionType)
        {
            IEnumerable<Dataset> datasets = await GetDatasetsByQuery(d => d.Id == specificationId);

            List<Dataset> datasetList = datasets.ToList();

            if (!datasetList.Any())
                return;

            if (deletionType == DeletionType.SoftDelete)
                await _cosmosRepository.BulkDeleteAsync(datasets.ToDictionary(c => c.Id), hardDelete:false);
            if (deletionType == DeletionType.PermanentDelete)
                await _cosmosRepository.BulkDeleteAsync(datasets.ToDictionary(c => c.Id), hardDelete:true);
        }

        public async Task DeleteDefinitionSpecificationRelationshipBySpecificationId(string specificationId, DeletionType deletionType)
        {
            IEnumerable<DefinitionSpecificationRelationship> relationships =
                 await GetDefinitionSpecificationRelationshipsByQuery(r => r.Content.Specification.Id == specificationId);

            List<DefinitionSpecificationRelationship> definitionSpecificationRelationshipList = relationships.ToList();

            if (!definitionSpecificationRelationshipList.Any())
                return;

            if (deletionType == DeletionType.SoftDelete)
                await _cosmosRepository.BulkDeleteAsync(definitionSpecificationRelationshipList.ToDictionary(c => c.Id), hardDelete:false);
            if (deletionType == DeletionType.PermanentDelete)
                await _cosmosRepository.BulkDeleteAsync(definitionSpecificationRelationshipList.ToDictionary(c => c.Id), hardDelete:true);
        }
    }
}
