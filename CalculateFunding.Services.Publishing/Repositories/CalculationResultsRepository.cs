using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Repositories
{
    public class CalculationResultsRepository : ICalculationResultsRepository, IHealthChecker
    {
        private readonly ICosmosRepository _cosmosRepository;

        public CalculationResultsRepository(ICosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(CalculationResultsRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = Message });

            return Task.FromResult(health);
        }

        public async Task<IEnumerable<ProviderCalculationResult>> GetCalculationResultsBySpecificationAndProvider(string specificationId, string providerId)
        {
            return await _cosmosRepository
                .DynamicQueryPartitionedEntity<ProviderCalculationResult>(new CosmosDbQuery
                {
                    QueryText = @"SELECT
	                                        doc.content.provider.id AS providerId,
	                                        ARRAY(SELECT calcResult.calculation.id,
	                                                       calcResult['value']
	                                                FROM   calcResult IN doc.content.calcResults) AS Results
                                        FROM 	doc
                                        WHERE   doc.documentType='ProviderResult'
                                        AND     doc.content.specificationId = @specificationId",                                       
                    Parameters = new[]
                         {
                             new CosmosDbQueryParameter("@specificationId", specificationId)                           
                         }
                },
                partitionEntityId: providerId);
        }
    }
}