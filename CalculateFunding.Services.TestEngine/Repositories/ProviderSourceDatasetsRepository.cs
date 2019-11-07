using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.TestRunner.Interfaces;

namespace CalculateFunding.Services.TestRunner.Repositories
{
    public class ProviderSourceDatasetsRepository : IProviderSourceDatasetsRepository, IHealthChecker
    {
        private readonly ICosmosRepository _cosmosRepository;
        private readonly EngineSettings _engineSettings;

        public ProviderSourceDatasetsRepository(ICosmosRepository cosmosRepository, EngineSettings engineSettings)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            Guard.ArgumentNotNull(engineSettings, nameof(engineSettings));

            _cosmosRepository = cosmosRepository;
            _engineSettings = engineSettings;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            (bool Ok, string Message) = _cosmosRepository.IsHealthOk();

            health.Name = nameof(ProviderSourceDatasetsRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = GetType().Name, Message = Message });

            return health;
        }

        public async Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsByProviderIdsAndSpecificationId(IEnumerable<string> providerIds, string specificationId)
        {
            if (providerIds.IsNullOrEmpty())
            {
                return Enumerable.Empty<ProviderSourceDataset>();
            }

            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ConcurrentBag<ProviderSourceDataset> results = new ConcurrentBag<ProviderSourceDataset>();

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
                            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
                            {
                                QueryText = @"SELECT *
                                            FROM    Root r
                                            WHERE   r.documentType = @DocumentType 
                                                    AND r.content.specificationId = @SpecificationId 
                                                    AND r.deleted = false",
                                Parameters = new[]
                                {
                                    new CosmosDbQueryParameter("@DocumentType", nameof(ProviderSourceDataset)),
                                    new CosmosDbQueryParameter("@SpecificationId", specificationId)
                                }
                            };

                            IEnumerable<ProviderSourceDataset> providerSourceDatasetResults =
                                await _cosmosRepository.QueryPartitionedEntity<ProviderSourceDataset>(cosmosDbQuery, partitionKey: providerId);

                            foreach (ProviderSourceDataset repoResult in providerSourceDatasetResults)
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
