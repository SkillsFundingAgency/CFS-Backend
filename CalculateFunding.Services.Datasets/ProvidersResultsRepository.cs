using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Linq;

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

        public Task<IEnumerable<ProviderSourceDatasetHistory>> GetProviderSourceDatasetHistories(string specificationId, string relationshipId)
        {
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = @"SELECT *
                            FROM    r 
                            WHERE   r.content.specificationId = @SpecificationId
                                    AND r.content.dataRelationship.id = @RelationshipId
                                    AND r.deleted = false
                                    AND r.documentType = @DocumentType",
                Parameters = new SqlParameterCollection
                {
                    new SqlParameter("@SpecificationId", specificationId),
                    new SqlParameter("@RelationshipId", relationshipId),
                    new SqlParameter("@DocumentType", nameof(ProviderSourceDatasetHistory))
                }
            };

            return _cosmosRepository.QuerySql<ProviderSourceDatasetHistory>(sqlQuerySpec, -1, enableCrossPartitionQuery: true);
        }

        public async Task<IEnumerable<ProviderSourceDataset>> GetCurrentProviderSourceDatasets(string specificationId, string relationshipId)
        {
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = @"SELECT *
                            FROM    r
                            WHERE   r.content.specificationId = @SpecificationId
                                    AND r.content.dataRelationship.id = @RelationshipId
                                    AND r.deleted = false
                                    AND r.documentType = @DocumentType",
                Parameters = new SqlParameterCollection
                {
                    new SqlParameter("@SpecificationId", specificationId),
                    new SqlParameter("@RelationshipId", relationshipId),
                    new SqlParameter("@DocumentType", nameof(ProviderSourceDataset))
                }
            };
            return await _cosmosRepository.QuerySql<ProviderSourceDataset>(sqlQuerySpec, enableCrossPartitionQuery: true, itemsPerPage: 1000);
        }

        public async Task DeleteCurrentProviderSourceDatasets(IEnumerable<ProviderSourceDataset> providerSourceDatasets)
        {
            Guard.ArgumentNotNull(providerSourceDatasets, nameof(providerSourceDatasets));

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 15);
            foreach (ProviderSourceDataset dataset in providerSourceDatasets)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            await _cosmosRepository.DeleteAsync<ProviderSourceDataset>(dataset.Id, enableCrossPartitionQuery: true);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await Task.WhenAll(allTasks);
        }

        public async Task UpdateCurrentProviderSourceDatasets(IEnumerable<ProviderSourceDataset> providerSourceDatasets)
        {
            Guard.ArgumentNotNull(providerSourceDatasets, nameof(providerSourceDatasets));

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 15);
            foreach (ProviderSourceDataset dataset in providerSourceDatasets)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            await _cosmosRepository.UpsertAsync(entity: dataset, partitionKey: dataset.ProviderId, undelete: true);
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
