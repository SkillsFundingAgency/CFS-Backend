using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Datasets.Interfaces;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Information;

namespace CalculateFunding.Services.Datasets
{
    public class ProviderSourceDatasetsRepository : IProviderSourceDatasetsRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;

        public ProviderSourceDatasetsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderSourceDatasetsRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = Message });

            return Task.FromResult(health);
        }

        public Task<IEnumerable<ProviderSourceDatasetHistory>> GetProviderSourceDatasetHistories(string specificationId, string relationshipId)
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT *
                            FROM    r 
                            WHERE   r.content.specificationId = @SpecificationId
                                    AND r.content.dataRelationship.id = @RelationshipId
                                    AND r.deleted = false
                                    AND r.documentType = @DocumentType",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@SpecificationId", specificationId),
                    new CosmosDbQueryParameter("@RelationshipId", relationshipId),
                    new CosmosDbQueryParameter("@DocumentType", nameof(ProviderSourceDatasetHistory))
                }
            };

            return _cosmosRepository.QuerySql<ProviderSourceDatasetHistory>(cosmosDbQuery, -1);
        }

        public async Task<IEnumerable<ProviderSourceDataset>> GetCurrentProviderSourceDatasets(string specificationId, string relationshipId)
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT *
                            FROM    r
                            WHERE   r.content.specificationId = @SpecificationId
                                    AND r.content.dataRelationship.id = @RelationshipId
                                    AND r.deleted = false
                                    AND r.documentType = @DocumentType",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@SpecificationId", specificationId),
                    new CosmosDbQueryParameter("@RelationshipId", relationshipId),
                    new CosmosDbQueryParameter("@DocumentType", nameof(ProviderSourceDataset))
                }
            };

            return await _cosmosRepository.QuerySql<ProviderSourceDataset>(cosmosDbQuery, itemsPerPage: 1000);
        }

        public async Task DeleteCurrentProviderSourceDatasets(IEnumerable<ProviderSourceDataset> providerSourceDatasets)
        {
            Guard.ArgumentNotNull(providerSourceDatasets, nameof(providerSourceDatasets));

            await _cosmosRepository.BulkDeleteAsync<ProviderSourceDataset>(
                providerSourceDatasets.Select(x => new KeyValuePair<string, ProviderSourceDataset>(x.ProviderId, x)),
                degreeOfParallelism: 15);
        }

        public async Task UpdateCurrentProviderSourceDatasets(IEnumerable<ProviderSourceDataset> providerSourceDatasets)
        {
            Guard.ArgumentNotNull(providerSourceDatasets, nameof(providerSourceDatasets));

            await _cosmosRepository.BulkUpsertAsync<ProviderSourceDataset>(
                providerSourceDatasets.Select(x => new KeyValuePair<string, ProviderSourceDataset>(x.ProviderId, x)),
                degreeOfParallelism: 15,
                undelete: true);
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
