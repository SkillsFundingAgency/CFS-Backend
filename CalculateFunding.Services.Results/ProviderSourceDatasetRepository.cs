using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Results.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
            var results = _cosmosRepository.Query<ProviderSourceDataset>(enableCrossPartitionQuery: true).Where(x => x.Provider.Id == providerId && x.Specification.Id == specificationId);

            return Task.FromResult(results.AsEnumerable());
        }

        public async Task<IEnumerable<string>> GetAllScopedProviderIdsForSpecificationid(string specificationId)
        {
            IEnumerable<DocumentEntity<ProviderSourceDataset>> providerSourceDatasets = await _cosmosRepository.GetAllDocumentsAsync<ProviderSourceDataset>(query: m => !m.Deleted && m.Content.Specification.Id == specificationId && m.DocumentType == nameof(ProviderSourceDataset));

            return providerSourceDatasets.Select(m => m.Content.Provider.Id);
        }
    }
}
