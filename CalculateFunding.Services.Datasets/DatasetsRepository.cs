using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Datasets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets
{
    public class DataSetsRepository : IDatasetRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public DataSetsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task<HttpStatusCode> SaveDefinition(DatasetDefinition definition)
        {
            return _cosmosRepository.CreateAsync(definition);
        }

        public Task<HttpStatusCode> SaveDataset(Dataset dataset)
        {
            return _cosmosRepository.CreateAsync(dataset);
        }

        async public Task<DatasetDefinition> GetDatasetDefinition(string definitionId)
        {
            var definitions = await GetDatasetDefinitionsByQuery(m => m.Id == definitionId);

            return definitions.FirstOrDefault();
        }

        async public Task<Dataset> GetDatasetByDatasetId(string datasetId)
        {
            var datasets = await GetDatasetsByQuery(m => m.Id == datasetId);

            return datasets.FirstOrDefault();
        }

        public Task<IEnumerable<DatasetDefinition>> GetDatasetDefinitions()
        {
            var definitions = _cosmosRepository.Query<DatasetDefinition>();

            return Task.FromResult(definitions.ToList().AsEnumerable());
        }

        public Task<IEnumerable<DatasetDefinition>> GetDatasetDefinitionsByQuery(Expression<Func<DatasetDefinition, bool>> query)
        {
            var definitions = _cosmosRepository.Query<DatasetDefinition>().Where(query);

            return Task.FromResult(definitions.AsEnumerable());
        }

        public Task<IEnumerable<Dataset>> GetDatasetsByQuery(Expression<Func<Dataset, bool>> query)
        {
            var datasets = _cosmosRepository.Query<Dataset>().Where(query);

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

        public Task<IEnumerable<DefinitionSpecificationRelationship>> GetDefinitionSpecificationRelationshipsByQuery(Expression<Func<DefinitionSpecificationRelationship, bool>> query)
        {
            var relationships = _cosmosRepository.Query<DefinitionSpecificationRelationship>().Where(query);

            return Task.FromResult(relationships.AsEnumerable());
        }

        async public Task<DefinitionSpecificationRelationship> GetRelationshipBySpecificationIdAndName(string specificationId, string name)
        {
            var relationships = await GetDefinitionSpecificationRelationshipsByQuery(m => m.Specification.Id == specificationId && m.Name == name);

            return relationships.FirstOrDefault();
        }

        async public Task<DefinitionSpecificationRelationship> GetDefinitionSpecificationRelationshipById(string relationshipId)
        {
            var relationships = await GetDefinitionSpecificationRelationshipsByQuery(m => m.Id == relationshipId);

            return relationships.FirstOrDefault();
        }
    }
}
