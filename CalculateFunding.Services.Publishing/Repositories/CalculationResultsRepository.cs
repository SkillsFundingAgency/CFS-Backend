using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.Documents;

namespace CalculateFunding.Services.Publishing.Repositories
{
    public class CalculationResultsRepository : ICalculationResultsRepository, IHealthChecker
    {
        private readonly ICosmosRepository _cosmosRepository;

        public CalculationResultsRepository(ICosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = await _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(CalculationResultsRepository)
            };
            health.Dependencies.Add(new DependencyHealth {HealthOk = Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = Message});

            return health;
        }


        public Task<IEnumerable<ProviderCalculationResult>> GetCalculationResultsBySpecificationId(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            IEnumerable<ProviderCalculationResult> providerResultSummaries = _cosmosRepository
                .DynamicQuery<ProviderCalculationResult>(new SqlQuerySpec
                {
                    QueryText = @"
SELECT
	    doc.content.id AS providerId,
	    ARRAY(  SELECT calcResult.calculation.id,
	                   calcResult['value']
	            FROM   calcResult IN doc.content.calcResults) AS Results
FROM 	doc
WHERE   doc.documentType='ProviderResult'
AND     doc.content.specificationId = @specificationId",
                    Parameters = new SqlParameterCollection
                    {
                        new SqlParameter("@specificationId", specificationId)
                    }
                }, true)
                .ToList();

            return Task.FromResult(providerResultSummaries);
        }
    }
}