using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.TestRunner.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Repositories
{
    public class ProviderResultsRepository : IProviderResultsRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public ProviderResultsRepository(CosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));

            _cosmosRepository = cosmosRepository;
        }

        public async Task<ProviderResult> GetProviderResultByProviderIdAndSpecificationId(string providerId, string specificationId)
        {
            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentNullException(nameof(providerId));

            if (string.IsNullOrWhiteSpace(specificationId))
                throw new ArgumentNullException(nameof(specificationId));

            string sql = $"select * from c where c.documentType = 'ProviderResult' and c.content.provider.id = '{providerId}' and c.content.specification.id = '{specificationId}'";

            ProviderResult providerResult = (await _cosmosRepository.QueryPartitionedEntity<ProviderResult>(sql, partitionEntityId: providerId)).FirstOrDefault();

            return providerResult;
        }
    }
}
