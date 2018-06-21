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

        public Task<HttpStatusCode> UpsertProviderSourceDataset(ProviderSourceDatasetCurrent providerSourceDataset)
        {
            return _cosmosRepository.CreateAsync(providerSourceDataset);
        }

        public Task<IEnumerable<ProviderSourceDatasetCurrent>> GetProviderSourceDatasets(string providerId, string specificationId)
        {
            return _cosmosRepository.QueryPartitionedEntity<ProviderSourceDatasetCurrent>($"SELECT * FROM Root r WHERE r.content.provider.id = '{providerId}' AND r.content.specificationId = '{specificationId}' AND r.documentType = '{nameof(ProviderSourceDatasetCurrent)}' & r.deleted = false", -1, specificationId);
        }

        public async Task<IEnumerable<string>> GetAllScopedProviderIdsForSpecificationId(string specificationId)
        {
            IEnumerable< ProviderSourceDatasetCurrent> providerSourceDatasets = await _cosmosRepository.QueryPartitionedEntity<ProviderSourceDatasetCurrent>($"SELECT * FROM Root r WHERE r.content.specificationId = '{specificationId}' AND r.documentType = '{nameof(ProviderSourceDatasetCurrent)}' & r.deleted = false", -1, specificationId);

            return providerSourceDatasets.Select(m => m.Provider.Id).Distinct();
        }
    }
}
