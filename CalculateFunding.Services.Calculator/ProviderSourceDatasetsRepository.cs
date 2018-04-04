using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Calculator.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calculator
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

        public Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasetsByProviderIdsAndSpecificationId(IEnumerable<string> providerIds, string specificationId)
        {
            string providerIdList = string.Join(",", providerIds.Select(m => $"\"{m}\""));

            string sql = $"SELECT * FROM Root r where r.documentType = \"ProviderSourceDataset\" and r.content.specification.id = \"{specificationId}\" and r.content.provider.id in ({providerIdList})";

            var results = _cosmosRepository.RawQuery<DocumentEntity<ProviderSourceDataset>>(sql);

            return Task.FromResult(results.AsEnumerable().Select(m => m.Content));
        }
    }
}
