using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.TestRunner.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Repositories
{
    public class ProviderSourceDatasetsRepository : IProviderSourceDatasetsRepository
    {
        private readonly CosmosRepository _cosmosRepository;
        private readonly ICacheProvider _cacheProvider;

        public ProviderSourceDatasetsRepository(CosmosRepository cosmosRepository, ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));

            _cosmosRepository = cosmosRepository;
            _cacheProvider = cacheProvider;
        }

        public async Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsByProviderIdsAndSpecificationId(IEnumerable<string> providerIds, string specificationId)
        {
            if (providerIds.IsNullOrEmpty())
            {
                return Enumerable.Empty<ProviderSourceDataset>();
            }

            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            List<Task<IEnumerable<ProviderSourceDataset>>> queryTasks = new List<Task<IEnumerable<ProviderSourceDataset>>>(providerIds.Count());
            foreach (string providerId in providerIds)
            {
                queryTasks.Add(_cosmosRepository.QueryPartitionedEntity<ProviderSourceDataset>($"SELECT * FROM Root r where r.documentType = 'ProviderSourceDataset' and r.content.specification.id = '{specificationId}' and r.content.provider.id ='{providerId}' AND r.deleted = false", partitionEntityId: providerId));
            }

            await TaskHelper.WhenAllAndThrow(queryTasks.ToArray());

            List<ProviderSourceDataset> result = new List<ProviderSourceDataset>();
            foreach (Task<IEnumerable<ProviderSourceDataset>> queryTask in queryTasks)
            {
                IEnumerable<ProviderSourceDataset> providerSourceDatasets = queryTask.Result;
                if (!providerSourceDatasets.IsNullOrEmpty())
                {

                    result.AddRange(providerSourceDatasets);
                }
            }

            return result;
        }
    }
}
