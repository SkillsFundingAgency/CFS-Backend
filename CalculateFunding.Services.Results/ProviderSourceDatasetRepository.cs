using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Services;
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

        public Task<IEnumerable<ProviderSourceDatasetCurrent>> GetProviderSourceDatasets(string providerId, string specificationId)
        {
            return _cosmosRepository.QueryPartitionedEntity<ProviderSourceDatasetCurrent>($"SELECT * FROM Root r WHERE r.content.providerId = '{providerId}' AND r.content.specificationId = '{specificationId}' AND r.documentType = '{nameof(ProviderSourceDatasetCurrent)}' AND r.deleted = false", -1, providerId);
        }

        public async Task<IEnumerable<string>> GetAllScopedProviderIdsForSpecificationId(string specificationId)
        {
            IEnumerable<dynamic> providerSourceDatasets = await _cosmosRepository.QueryDynamic<dynamic>($"SELECT r.content.providerId FROM Root r WHERE r.content.specificationId = '{specificationId}' AND r.documentType = '{nameof(ProviderSourceDatasetCurrent)}' AND r.deleted = false", true);

            IEnumerable<string> providerIds = providerSourceDatasets.Select(m => new string(m.providerId)).Distinct();

            return providerIds;
        }
    }
}
