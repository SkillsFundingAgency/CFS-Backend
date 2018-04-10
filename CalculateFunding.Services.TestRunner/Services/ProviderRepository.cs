using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.TestRunner.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class ProviderRepository : IProviderRepository
    {
        private readonly CosmosRepository _cosmosRepository;
        private readonly ICacheProvider _cacheProvider;

        public ProviderRepository(CosmosRepository cosmosRepository, ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));

            _cosmosRepository = cosmosRepository;
            _cacheProvider = cacheProvider;
        }

        public async Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsBySpecificationId(string specificationId)
        {
            IEnumerable<ProviderSourceDataset> sourceDatasets = await _cacheProvider.GetAsync<List<ProviderSourceDataset>>(specificationId);

            if (sourceDatasets == null)
            {
                IQueryable<ProviderSourceDataset> datasets = _cosmosRepository.Query<ProviderSourceDataset>().Where(m => m.Specification.Id == specificationId);

                sourceDatasets = datasets.AsEnumerable();

                await _cacheProvider.SetAsync<List<ProviderSourceDataset>>(specificationId, sourceDatasets.ToList(), TimeSpan.FromHours(1), false);
            }

            return sourceDatasets;
        }
    }
}
