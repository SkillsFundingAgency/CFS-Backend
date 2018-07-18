using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.TestRunner.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Repositories
{
    public class ProviderSourceDatasetsRepository : IProviderSourceDatasetsRepository
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

        public async Task<IEnumerable<ProviderSourceDatasetCurrent>> GetProviderSourceDatasetsByProviderIdsAndSpecificationId(IEnumerable<string> providerIds, string specificationId)
        {
            if (providerIds.IsNullOrEmpty())
            {
                return Enumerable.Empty<ProviderSourceDatasetCurrent>();
            }

            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ConcurrentBag<ProviderSourceDatasetCurrent> results = new ConcurrentBag<ProviderSourceDatasetCurrent>();

            int completedCount = 0;

            ParallelLoopResult result = Parallel.ForEach(providerIds, new ParallelOptions() { MaxDegreeOfParallelism = _engineSettings.GetProviderSourceDatasetsDegreeOfParallelism }, async (providerId) =>
            {
                string sql = $"SELECT * FROM Root r where r.documentType = '{nameof(ProviderSourceDatasetCurrent)}' and r.content.specificationId = '{specificationId}' and r.content.providerId ='{providerId}' AND r.deleted = false";
                IEnumerable<ProviderSourceDatasetCurrent> providerSourceDatasetResults = await _cosmosRepository.QueryPartitionedEntity<ProviderSourceDatasetCurrent>(sql, partitionEntityId: specificationId);
                foreach (ProviderSourceDatasetCurrent repoResult in providerSourceDatasetResults)
                {
                    results.Add(repoResult);
                }
                completedCount++;
            });

            while (completedCount < providerIds.Count())
            {
                await Task.Delay(20);
            }

            return results.AsEnumerable();
        }
    }
}
