﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.Documents;

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

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            (bool Ok, string Message) cosmosHealth = await _cosmosRepository.IsHealthOk();

            health.Name = nameof(DataSetsRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosHealth.Ok, DependencyName = this.GetType().Name, Message = cosmosHealth.Message });

            return health;
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

        public Task<IEnumerable<DatasetDefinition>> GetDatasetDefinitions()
        {
            IQueryable<DatasetDefinition> definitions = _cosmosRepository.Query<DatasetDefinition>();

            return Task.FromResult(definitions.ToList().AsEnumerable());
        }

        public Task<IEnumerable<DatasetDefinition>> GetDatasetDefinitionsByQuery(Expression<Func<DatasetDefinition, bool>> query)
        {
            IQueryable<DatasetDefinition> definitions = _cosmosRepository.Query<DatasetDefinition>().Where(query);

            return Task.FromResult(definitions.AsEnumerable());
        }

        public Task<IEnumerable<Dataset>> GetDatasetsByQuery(Expression<Func<Dataset, bool>> query)
        {
            IQueryable<Dataset> datasets = _cosmosRepository.Query<Dataset>().Where(query);

            return Task.FromResult(datasets.AsEnumerable());
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

        public Task<IEnumerable<DefinitionSpecificationRelationship>> GetDefinitionSpecificationRelationshipsByQuery(Expression<Func<DefinitionSpecificationRelationship, bool>> query)
        {
            IQueryable<DefinitionSpecificationRelationship> relationships = _cosmosRepository.Query<DefinitionSpecificationRelationship>().Where(query);

            return Task.FromResult(relationships.AsEnumerable());
        }

        public async Task<DefinitionSpecificationRelationship> GetRelationshipBySpecificationIdAndName(string specificationId, string name)
        {
            IEnumerable<DefinitionSpecificationRelationship> relationships = await GetDefinitionSpecificationRelationshipsByQuery(m => m.Specification.Id == specificationId);

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

        public Task<DocumentEntity<Dataset>> GetDatasetDocumentByDatasetId(string datasetId)
        {
            DocumentEntity<Dataset> dataset = _cosmosRepository.QueryDocuments<Dataset>(1).Where(c => c.Id == datasetId && !c.Deleted).AsEnumerable().FirstOrDefault();

            return Task.FromResult(dataset);
        }

        public Task<IEnumerable<DefinitionSpecificationRelationship>> GetAllDefinitionSpecificationsRelationships()
        {
            IQueryable<DefinitionSpecificationRelationship> relationships = _cosmosRepository.Query<DefinitionSpecificationRelationship>();

            return Task.FromResult(relationships.AsEnumerable());
        }

        public async Task<IEnumerable<string>> GetDistinctRelationshipSpecificationIdsForDatasetDefinitionId(string datasetDefinitionId)
        {
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = @"SELECT d.content.Specification.id AS specificationId
                            FROM    datasets d
                            WHERE   d.deleted = false 
                                    AND d.documentType = ""DefinitionSpecificationRelationship"" 
                                    AND d.content.DatasetDefinition.id = @DatasetDefinitionId",
                Parameters = new SqlParameterCollection
                {
                    new SqlParameter("@DatasetDefinitionID", datasetDefinitionId)
                }
            };

            HashSet<string> specificationIds = new HashSet<string>();

            IEnumerable<dynamic> results = await _cosmosRepository.QueryDynamic(sqlQuerySpec, true, 1000);

            foreach (dynamic result in results)
            {
                specificationIds.Add(result.specificationId);
            }

            return specificationIds;
        }
    }
}
