using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Results.Interfaces;

namespace CalculateFunding.Services.Results
{
    public class ResultsRepository : IResultsRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public ResultsRepository(CosmosRepository cosmosRepository)
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
    }
}
