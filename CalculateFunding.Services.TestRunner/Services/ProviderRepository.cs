using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Proxies;
using CalculateFunding.Services.TestRunner.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class ProviderRepository : IProviderRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public ProviderRepository(CosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));

            _cosmosRepository = cosmosRepository;
        }

        public Task<ProviderResult> GetProviderByIdAndSpecificationId(string providerId, string specificationId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentNullException(nameof(providerId));

            ProviderResult providerResult = _cosmosRepository.Query<ProviderResult>().Where(m => m.Provider.Id == providerId && m.Specification.Id == specificationId).FirstOrDefault();

            return Task.FromResult(providerResult);
        }

        public Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsBySpecificationId(string specificationId)
        {
            IQueryable<ProviderSourceDataset> sourceDatasets = _cosmosRepository.Query<ProviderSourceDataset>().Where(m => m.Specification.Id == specificationId);

            return Task.FromResult(sourceDatasets.AsEnumerable());
        }
    }
}
