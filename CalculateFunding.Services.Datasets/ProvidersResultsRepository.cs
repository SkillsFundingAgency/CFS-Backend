using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Datasets.Interfaces;

namespace CalculateFunding.Services.Datasets
{
    public class ProvidersResultsRepository : IProvidersResultsRepository
    {
        private readonly CosmosRepository _cosmosRepository;
        private readonly ICacheProvider _cacheProvider;

        public ProvidersResultsRepository(CosmosRepository cosmosRepository, ICacheProvider cacheProvider)
        {
            _cosmosRepository = cosmosRepository;
            _cacheProvider = cacheProvider;
        }

        public async Task UpdateCurrentProviderSourceDatasets(IEnumerable<ProviderSourceDatasetCurrent> providerSourceDatasets, string specificationId)
        {
            await _cacheProvider.RemoveAsync<List<ProviderSourceDatasetCurrent>>(specificationId);

            IEnumerable<KeyValuePair<string, ProviderSourceDatasetCurrent>> datasets = providerSourceDatasets.Select(m => new KeyValuePair<string, ProviderSourceDatasetCurrent>(specificationId, m));

            await _cosmosRepository.BulkCreateAsync(datasets);
        }

        public async Task UpdateProviderSourceDatasetHistory(IEnumerable<ProviderSourceDatasetHistory> providerSourceDatasets, string specificationId)
        {
            IEnumerable<KeyValuePair<string, ProviderSourceDatasetHistory>> datasets = providerSourceDatasets.Select(m => new KeyValuePair<string, ProviderSourceDatasetHistory>(specificationId, m));

            await _cosmosRepository.BulkCreateAsync(datasets);
        }

        public async Task<IEnumerable<string>> GetAllProviderIdsForSpecificationid(string specificationId)
        {
            IEnumerable<ProviderSourceDatasetCurrent> providerSourceDatasets = await _cosmosRepository.QueryPartitionedEntity<ProviderSourceDatasetCurrent>($"SELECT * FROM r WHERE r.content.specificationId = '{specificationId}' AND r.deleted = false AND r.documentType = '{nameof(ProviderSourceDatasetCurrent)}'", -1, specificationId);

            return providerSourceDatasets.Select(m => m.ProviderId);
        }

        public Task<IEnumerable<ProviderSourceDatasetHistory>> GetProviderSourceDatasetHistories(string specificationId, string relationshipId)
        {
            return _cosmosRepository.QueryPartitionedEntity<ProviderSourceDatasetHistory>($"SELECT * FROM r WHERE r.content.specificationId = '{specificationId}' AND r.content.dataRelationship.id = '{relationshipId}' AND r.deleted = false AND r.documentType = '{nameof(ProviderSourceDatasetHistory)}'", -1, specificationId);

        }

        public Task<IEnumerable<ProviderSourceDatasetCurrent>> GetCurrentProviderSourceDatasets(string specificationId, string relationshipId)
        {
            return _cosmosRepository.QueryPartitionedEntity<ProviderSourceDatasetCurrent>($"SELECT * FROM r WHERE r.content.specificationId = '{specificationId}' AND r.content.dataRelationship.id = '{relationshipId}' AND r.deleted = false AND r.documentType = '{nameof(ProviderSourceDatasetCurrent)}'", -1, specificationId);
        }
    }
}
