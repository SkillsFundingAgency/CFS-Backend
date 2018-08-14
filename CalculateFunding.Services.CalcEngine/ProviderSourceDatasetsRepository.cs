using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;

namespace CalculateFunding.Services.Calculator
{
    public class ProviderSourceDatasetsRepository : IProviderSourceDatasetsRepository
    {
        private readonly CosmosRepository _cosmosRepository;
        private readonly EngineSettings _engineSettings;

        public ProviderSourceDatasetsRepository(CosmosRepository cosmosRepository, EngineSettings engineSettings)
        {
            _cosmosRepository = cosmosRepository;
            _engineSettings = engineSettings;
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
