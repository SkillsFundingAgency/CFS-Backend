using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsByProviderIdsAndSpecificationId(IEnumerable<string> providerIds, string specificationId)
        {
            if (providerIds.IsNullOrEmpty())
            {
                return Enumerable.Empty<ProviderSourceDataset>();
            }

            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ConcurrentBag<ProviderSourceDataset> results = new ConcurrentBag<ProviderSourceDataset>();

            int completedCount = 0;

            ParallelLoopResult result = Parallel.ForEach(providerIds, new ParallelOptions() { MaxDegreeOfParallelism = _engineSettings.GetProviderSourceDatasetsDegreeOfParallelism }, async (providerId) =>
            {
                string sql = $"SELECT * FROM Root r where r.documentType = 'ProviderSourceDataset' and r.content.specification.id = '{specificationId}' and r.content.provider.id ='{providerId}' AND r.deleted = false";
                IEnumerable<ProviderSourceDataset> providerSourceDatasetResults = await _cosmosRepository.QueryPartitionedEntity<ProviderSourceDataset>(sql, partitionEntityId: providerId);
                foreach (ProviderSourceDataset repoResult in providerSourceDatasetResults)
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
