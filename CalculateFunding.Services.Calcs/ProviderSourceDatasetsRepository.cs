using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Calcs.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class ProviderSourceDatasetsRepository : IProviderSourceDatasetsRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public ProviderSourceDatasetsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsByProviderIdAndSpecificationId(string providerId, string specificationId)
        {
            var results = _cosmosRepository.Query<ProviderSourceDataset>().Where(x => x.Provider.Id == providerId && x.Specification.Id == specificationId);

            return Task.FromResult(results.AsEnumerable());
        }
    }
}
