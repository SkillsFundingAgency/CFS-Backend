using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.TestRunner.Interfaces;

namespace CalculateFunding.Services.TestRunner.Repositories
{
    public class ProviderResultsRepository : IProviderResultsRepository, IHealthChecker
    {
        private readonly ICosmosRepository _cosmosRepository;

        public ProviderResultsRepository(ICosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));

            _cosmosRepository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            (bool Ok, string Message) = _cosmosRepository.IsHealthOk();

            health.Name = GetType().Name;
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = typeof(CosmosRepository).Name, Message = Message });

            return health;
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
