using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Datasets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets
{
    public class ProvidersResultsRepository : IProvidersResultsRepository
    {
        private readonly CosmosRepository _cosmosRepository;
        private readonly ICacheProvider _cacheProvider;

        public ProvidersResultsRepository(CosmosRepository cosmosRepository, ICacheProvider cacheProvider)
        {
            _cosmosRepository = cosmosRepository;
            _cacheProvider = cacheProvider;
        }

        public async Task UpdateCurrentSourceDatsets(IEnumerable<ProviderSourceDatasetCurrent> providerSourceDatasets, string specificationId)
        {
            // TODO - use polly to retry instead of sequentially
            try
            {
                await _cacheProvider.RemoveAsync<List<ProviderSourceDatasetCurrent>>(specificationId);

                await _cosmosRepository.BulkCreateAsync(providerSourceDatasets.ToList());
            }
            catch (Exception ex)
            {
                
                foreach (ProviderSourceDatasetCurrent sourceDataset in providerSourceDatasets)
                {
                    await _cosmosRepository.CreateAsync(sourceDataset);
                }
            }
        }

        public async Task UpdateSourceDatasetHistory(IEnumerable<ProviderSourceDatasetHistory> providerSourceDatasets, string specificationId)
        {
            try
            {
                await _cosmosRepository.BulkCreateAsync(providerSourceDatasets.ToList());
            }
            catch (Exception ex)
            {

                foreach (ProviderSourceDatasetHistory sourceDataset in providerSourceDatasets)
                {
                    await _cosmosRepository.CreateAsync(sourceDataset);
                }
            }
        }

        public async Task<IEnumerable<string>> GetAllProviderIdsForSpecificationid(string specificationId)
        {
            IEnumerable<DocumentEntity<ProviderSourceDatasetCurrent>> providerSourceDatasets = await _cosmosRepository.GetAllDocumentsAsync<ProviderSourceDatasetCurrent>(query: m => !m.Deleted && m.Content.SpecificationId == specificationId);

            return providerSourceDatasets.Select(m => m.Content.Provider.Id);
        }
    }
}
