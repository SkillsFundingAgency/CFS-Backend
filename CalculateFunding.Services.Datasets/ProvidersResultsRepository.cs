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

        public async Task UpdateSourceDatsets(IEnumerable<ProviderSourceDataset> providerSourceDatasets, string specificationId)
        {
            try
            {
                await _cacheProvider.RemoveAsync<List<ProviderSourceDataset>>(specificationId);

                await _cosmosRepository.BulkCreateAsync(providerSourceDatasets.ToList());
            }
            catch (Exception ex)
            {
                
                foreach (ProviderSourceDataset sourceDataset in providerSourceDatasets)
                {
                    await _cosmosRepository.CreateAsync(sourceDataset);
                }
            }
        }
        
        public async Task<IEnumerable<string>> GetAllProviderIdsForSpecificationid(string specificationId)
        {
            IEnumerable<DocumentEntity<ProviderSourceDataset>> providerSourceDatasets = await _cosmosRepository.GetAllDocumentsAsync<ProviderSourceDataset>(query: m => !m.Deleted && m.Content.Specification.Id == specificationId && m.DocumentType == "ProviderSourceDataset");

            return providerSourceDatasets.Select(m => m.Content.Provider.Id);

        }
    }
}
