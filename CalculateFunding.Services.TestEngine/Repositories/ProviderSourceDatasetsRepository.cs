using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.Azure.Documents;

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

            var cosmosHealth = await _cosmosRepository.IsHealthOk();

            health.Name = nameof(ProviderSourceDatasetsRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosHealth.Ok, DependencyName = this.GetType().Name, Message = cosmosHealth.Message });

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
                            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
                            {
                                QueryText =  @"SELECT *
                                            FROM    Root r
                                            WHERE   r.documentType = @DocumentType 
                                                    AND r.content.specificationId = @SpecificationId 
                                                    AND r.content.providerId = @ProviderId 
                                                    AND r.deleted = false",
                                Parameters = new SqlParameterCollection
                                {
                                    new SqlParameter("@DocumentType", nameof(ProviderSourceDataset)),
                                    new SqlParameter("@SpecificationId", specificationId),
                                    new SqlParameter("@ProviderId", providerId)
                                }
                            };

                            IEnumerable<ProviderSourceDataset> providerSourceDatasetResults = 
                                await _cosmosRepository.QueryPartitionedEntity<ProviderSourceDataset>(sqlQuerySpec, partitionEntityId: providerId);

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
