using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.TestRunner.Interfaces;

namespace CalculateFunding.Services.TestRunner.Repositories
{
    public class ProviderSourceDatasetsRepository : IProviderSourceDatasetsRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;
        private readonly EngineSettings _engineSettings;

        public ProviderSourceDatasetsRepository(CosmosRepository cosmosRepository, EngineSettings engineSettings)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(engineSettings, nameof(engineSettings));

            _cosmosRepository = cosmosRepository;
            _engineSettings = engineSettings;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            var cosmosHealth = await _cosmosRepository.IsHealthOk();

            health.Name = nameof(ProviderSourceDatasetsRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosHealth.Ok, DependencyName = this.GetType().Name, Message = cosmosHealth.Message });

            return health;
        }

        public async Task<IEnumerable<ProviderSourceDatasetCurrent>> GetProviderSourceDatasetsByProviderIdsAndSpecificationId(IEnumerable<string> providerIds, string specificationId)
        {
            if (providerIds.IsNullOrEmpty())
            {
                return Enumerable.Empty<ProviderSourceDatasetCurrent>();
            }

            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ConcurrentBag<ProviderSourceDatasetCurrent> results = new ConcurrentBag<ProviderSourceDatasetCurrent>();

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: _engineSettings.GetProviderSourceDatasetsDegreeOfParallelism);
            foreach (string providerId in providerIds)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            string sql = $"SELECT * FROM Root r where r.documentType = '{nameof(ProviderSourceDatasetCurrent)}' and r.content.specificationId = '{specificationId}' and r.content.providerId ='{providerId}' AND r.deleted = false";
                            IEnumerable<ProviderSourceDatasetCurrent> providerSourceDatasetResults = await _cosmosRepository.QueryPartitionedEntity<ProviderSourceDatasetCurrent>(sql, partitionEntityId: providerId);
                            foreach (ProviderSourceDatasetCurrent repoResult in providerSourceDatasetResults)
                            {
                                results.Add(repoResult);
                            }
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await Task.WhenAll(allTasks);

            return results.AsEnumerable();
        }
    }
}
