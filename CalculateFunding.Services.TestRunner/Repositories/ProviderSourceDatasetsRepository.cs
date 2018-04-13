using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Options;
using CalculateFunding.Services.TestRunner.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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

        public Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsByProviderIdsAndSpecificationId(IEnumerable<string> providerIds, string specificationId)
        {
            if (providerIds.IsNullOrEmpty())
            {
                return Task.FromResult(Enumerable.Empty<ProviderSourceDataset>());
            }

            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ConcurrentBag<ProviderSourceDataset> results = new ConcurrentBag<ProviderSourceDataset>();

            ParallelLoopResult result = Parallel.ForEach(providerIds, new ParallelOptions() { MaxDegreeOfParallelism = _engineSettings.GetProviderSourceDatasetsDegreeOfParallelism }, async (providerId) =>
            {
                string sql = $"SELECT * FROM Root r where r.documentType = 'ProviderSourceDataset' and r.content.specification.id = '{specificationId}' and r.content.provider.id ='{providerId}' AND r.deleted = false";
                IEnumerable<ProviderSourceDataset> providerSourceDatasetResults = await _cosmosRepository.QueryPartitionedEntity<ProviderSourceDataset>(sql, partitionEntityId: providerId);
                foreach (ProviderSourceDataset repoResult in providerSourceDatasetResults)
                {
                    results.Add(repoResult);
                }
            });

            return Task.FromResult(results.AsEnumerable());
        }
    }
}
