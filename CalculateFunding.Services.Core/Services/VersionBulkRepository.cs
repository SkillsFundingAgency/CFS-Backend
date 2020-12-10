using CalculateFunding.Models.Versioning;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces.Services;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Services.Core.Extensions;

namespace CalculateFunding.Services.Core.Services
{
    public class VersionBulkRepository<T> : IVersionBulkRepository<T> where T : VersionedItem
    {
        protected readonly ICosmosRepository _cosmosRepository;

        public VersionBulkRepository(ICosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));

            _cosmosRepository = cosmosRepository;
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(VersionBulkRepository<T>)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = Message });

            return Task.FromResult(health);
        }

        public async Task<T> SaveVersion(T newVersion, string partitionKey)
        {
            await _cosmosRepository.UpsertAsync(newVersion, partitionKey);
            
            return newVersion;
        }
    }
}
