using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets
{
    public class DataSetsRepository : IDataSetsRepository
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
    }
}
