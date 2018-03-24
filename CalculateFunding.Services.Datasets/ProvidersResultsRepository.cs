using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Datasets.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Datasets
{
    public class ProvidersResultsRepository : IProvidersResultsRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public ProvidersResultsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        async public Task UpdateSourceDatsets(IEnumerable<ProviderSourceDataset> providerSourceDatasets)
        {
            try
            { 
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

        
    }
}
