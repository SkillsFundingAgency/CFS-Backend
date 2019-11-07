using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.TestRunner.Interfaces;

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
            {
                throw new ArgumentNullException(nameof(providerId));
            }

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                throw new ArgumentNullException(nameof(specificationId));
            }

            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT *
                            FROM    c 
                            WHERE   c.documentType = 'ProviderResult'
                                    AND c.content.specification.id = @SpecificationId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@SpecificationId", specificationId)
                }
            };

            ProviderResult providerResult = (await _cosmosRepository.QueryPartitionedEntity<ProviderResult>(cosmosDbQuery, partitionKey: providerId)).FirstOrDefault();

            return providerResult;
        }
    }
}
