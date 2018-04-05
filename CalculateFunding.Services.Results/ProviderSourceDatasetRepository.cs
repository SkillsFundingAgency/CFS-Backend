using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Results.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class ProviderSourceDatasetRepository : IProviderSourceDatasetRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public ProviderSourceDatasetRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task<HttpStatusCode> UpsertProviderSourceDataset(ProviderSourceDataset providerSourceDataset)
        {
            return _cosmosRepository.CreateAsync(providerSourceDataset);
        }

        public Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasets(string providerId, string specificationId)
        {
            var results = _cosmosRepository.Query<ProviderSourceDataset>().Where(x => x.Provider.Id == providerId && x.Specification.Id == specificationId);

            return Task.FromResult(results.AsEnumerable());
        }
    }
}
