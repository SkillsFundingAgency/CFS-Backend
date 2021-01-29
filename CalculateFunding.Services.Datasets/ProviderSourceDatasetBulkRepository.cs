using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Helpers;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;

namespace CalculateFunding.Services.Datasets
{
    public class ProviderSourceDatasetBulkRepository : IProviderSourceDatasetBulkRepository
    {
        private readonly ICosmosRepository _cosmosRepository;

        public ProviderSourceDatasetBulkRepository(ICosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));

            _cosmosRepository = cosmosRepository;
        }

        public async Task DeleteCurrentProviderSourceDatasets(IEnumerable<ProviderSourceDataset> providerSourceDatasets)
        {
            Guard.ArgumentNotNull(providerSourceDatasets, nameof(providerSourceDatasets));

            //keep getting a bad request when I use this delete path with allow bulk set to true so reverted to existing bulk delete method for now

            // Task<HttpStatusCode>[] deleteTasks = providerSourceDatasets
            //     .Select(providerSourceDataset => _cosmosRepository.DeleteAsync<ProviderSourceDataset>(providerSourceDataset.Id, providerSourceDataset.ProviderId, true))
            //     .ToArray();
            //
            // await TaskHelper.WhenAllAndThrow(deleteTasks);

            await _cosmosRepository.BulkDeleteAsync(providerSourceDatasets.Select(_ => new KeyValuePair<string, ProviderSourceDataset>(_.ProviderId, _)));
        }

        public async Task UpdateCurrentProviderSourceDatasets(IEnumerable<ProviderSourceDataset> providerSourceDatasets)
        {
            Guard.ArgumentNotNull(providerSourceDatasets, nameof(providerSourceDatasets));

            Task<HttpStatusCode>[] upsertTasks = providerSourceDatasets
                .Select(providerSourceDataset => _cosmosRepository.UpsertAsync(providerSourceDataset, providerSourceDataset.ProviderId, undelete: true))
                .ToArray();

            await TaskHelper.WhenAllAndThrow(upsertTasks);
        }

        public async Task UpdateProviderSourceDatasetHistory(IEnumerable<ProviderSourceDatasetHistory> providerSourceDatasets)
        {
            Guard.ArgumentNotNull(providerSourceDatasets, nameof(providerSourceDatasets));

            await _cosmosRepository.BulkCreateAsync(providerSourceDatasets.Select(_ => new KeyValuePair<string, ProviderSourceDatasetHistory>(_.ProviderId, _)));

            //got a stack overflow when I used the allow bulk option with this call so walked it back to the original bulk create call

            // Task<HttpStatusCode>[] createTasks = providerSourceDatasets
            //     .Select(providerSourceDataset =>
            //         _cosmosRepository.CreateAsync(new KeyValuePair<string, ProviderSourceDatasetHistory>(providerSourceDataset.ProviderId, providerSourceDataset)))
            //     .ToArray();
            //
            // await TaskHelper.WhenAllAndThrow(createTasks);
        }
    }
}