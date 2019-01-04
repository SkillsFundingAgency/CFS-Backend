using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Results.Interfaces;

namespace CalculateFunding.Services.Results
{
    public class ProviderSourceDatasetRepository : IProviderSourceDatasetRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;

        public ProviderSourceDatasetRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var cosmosRepoHealth = await _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderSourceDatasetRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
        }

        public Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasets(string providerId, string specificationId)
        {
            return _cosmosRepository.QueryPartitionedEntity<ProviderSourceDataset>($"SELECT * FROM Root r WHERE r.content.providerId = '{providerId}' AND r.content.specificationId = '{specificationId}' AND r.documentType = '{nameof(ProviderSourceDataset)}' AND r.deleted = false", -1, providerId);
        }

        public async Task<IEnumerable<string>> GetAllScopedProviderIdsForSpecificationId(string specificationId)
        {
            IEnumerable<dynamic> providerSourceDatasets = await _cosmosRepository.QueryDynamic<dynamic>($"SELECT r.content.providerId FROM Root r WHERE r.content.specificationId = '{specificationId}' AND r.documentType = '{nameof(ProviderSourceDataset)}' AND r.deleted = false", true);

            IEnumerable<string> providerIds = providerSourceDatasets.Select(m => new string(m.providerId)).Distinct();

            return providerIds;
        }
    }
}
