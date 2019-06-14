using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.Documents;

namespace CalculateFunding.Services.Results.Repositories
{
    public class ProviderSourceDatasetRepository : IProviderSourceDatasetRepository, IHealthChecker
    {
        private readonly ICosmosRepository _cosmosRepository;

        public ProviderSourceDatasetRepository(ICosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) cosmosRepoHealth = await _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderSourceDatasetRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
        }

        public Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasets(string providerId, string specificationId)
        {
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = @"SELECT *
                            FROM    Root r
                            WHERE   r.content.providerId = @ProviderId
                                    AND r.content.specificationId = @SpecificationId
                                    AND r.documentType = @DocumentType 
                                    AND r.deleted = false",
                Parameters = new SqlParameterCollection
                {
                      new SqlParameter("@ProviderId", providerId),
                      new SqlParameter("@SpecificationId", specificationId), 
                      new SqlParameter("@DocumentType", nameof(ProviderSourceDataset))
                }
            };

            return _cosmosRepository.QueryPartitionedEntity<ProviderSourceDataset>(sqlQuerySpec, -1, providerId);
        }

        public async Task<IEnumerable<string>> GetAllScopedProviderIdsForSpecificationId(string specificationId)
        {
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = @"SELECT    r.content.providerId
                            FROM        Root r 
                            WHERE       r.content.specificationId = @SpecificationId
                                        AND r.documentType = @DocumentType
                                        AND r.deleted = false 
                                        AND r.content.definesScope = true",
                Parameters = new SqlParameterCollection
                {
                    new SqlParameter("@SpecificationId", specificationId),
                    new SqlParameter("@DocumentType", nameof(ProviderSourceDataset))
                }
            };

            IEnumerable<dynamic> providerSourceDatasets = await _cosmosRepository.QueryDynamic(sqlQuerySpec, true);

            IEnumerable<string> providerIds = providerSourceDatasets.Select(m => new string(m.providerId)).Distinct();

            return providerIds;
        }
    }
}
