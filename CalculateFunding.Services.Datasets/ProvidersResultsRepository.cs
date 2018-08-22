using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Datasets.Interfaces;

namespace CalculateFunding.Services.Datasets
{
    public class ProvidersResultsRepository : IProvidersResultsRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;

        public ProvidersResultsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var cosmosRepoHealth = await _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProvidersResultsRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
        }

        public async Task<IEnumerable<string>> GetAllProviderIdsForSpecificationid(string specificationId)
        {
            IEnumerable<ProviderSourceDatasetCurrent> providerSourceDatasets = await _cosmosRepository.QueryPartitionedEntity<ProviderSourceDatasetCurrent>($"SELECT * FROM r WHERE r.content.specificationId = '{specificationId}' AND r.deleted = false AND r.documentType = '{nameof(ProviderSourceDatasetCurrent)}'", -1, specificationId);

            return providerSourceDatasets.Select(m => m.ProviderId).Distinct();
        }

        public Task<IEnumerable<ProviderSourceDatasetHistory>> GetProviderSourceDatasetHistories(string specificationId, string relationshipId)
        {
            return _cosmosRepository.QuerySql<ProviderSourceDatasetHistory>($"SELECT * FROM r WHERE r.content.specificationId = '{specificationId}' AND r.content.dataRelationship.id = '{relationshipId}' AND r.deleted = false AND r.documentType = '{nameof(ProviderSourceDatasetHistory)}'", -1, enableCrossPartitionQuery: true);

        }

        public async Task<IEnumerable<ProviderSourceDatasetCurrent>> GetCurrentProviderSourceDatasets(string specificationId, string relationshipId)
        {
            return await _cosmosRepository.QuerySql<ProviderSourceDatasetCurrent>($"SELECT * FROM r WHERE r.content.specificationId = '{specificationId}' AND r.content.dataRelationship.id = '{relationshipId}' AND r.deleted = false AND r.documentType = '{nameof(ProviderSourceDatasetCurrent)}'", enableCrossPartitionQuery: true, itemsPerPage: 1000);
        }

        public async Task UpdateCurrentProviderSourceDatasets(IEnumerable<ProviderSourceDatasetCurrent> providerSourceDatasets)
        {
            Guard.ArgumentNotNull(providerSourceDatasets, nameof(providerSourceDatasets));

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 15);
            foreach (ProviderSourceDatasetCurrent dataset in providerSourceDatasets)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            await _cosmosRepository.CreateAsync(new KeyValuePair<string, ProviderSourceDatasetCurrent>(dataset.ProviderId, dataset));
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await Task.WhenAll(allTasks);
        }

        public async Task UpdateProviderSourceDatasetHistory(IEnumerable<ProviderSourceDatasetHistory> providerSourceDatasets)
        {
            Guard.ArgumentNotNull(providerSourceDatasets, nameof(providerSourceDatasets));

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 15);
            foreach (ProviderSourceDatasetHistory dataset in providerSourceDatasets)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            await _cosmosRepository.CreateAsync(new KeyValuePair<string, ProviderSourceDatasetHistory>(dataset.ProviderId, dataset));
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await Task.WhenAll(allTasks);
        }
    }
}
