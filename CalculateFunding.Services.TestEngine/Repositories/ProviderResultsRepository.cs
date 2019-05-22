using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.Azure.Documents;

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

            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = @"SELECT *
                            FROM    c 
                            WHERE   c.documentType = 'ProviderResult'
                                    AND c.content.specification.id = @SpecificationId",
                Parameters = new SqlParameterCollection
                {
                    new SqlParameter("@SpecificationId", specificationId)
                }
            };

            ProviderResult providerResult = (await _cosmosRepository.QueryPartitionedEntity<ProviderResult>(sqlQuerySpec, partitionEntityId: providerId)).FirstOrDefault();

            return providerResult;
        }
    }
}
