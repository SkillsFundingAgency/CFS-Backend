using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Datasets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets
{
    public class ProvidersResultsRepository : IProvidersResultsRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public ProvidersResultsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task UpdateSourceDatsets(IEnumerable<ProviderSourceDataset> providerSourceDatasets)
        {
            return _cosmosRepository.BulkCreateAsync(providerSourceDatasets.ToList(), 10);
        }
    }
}
