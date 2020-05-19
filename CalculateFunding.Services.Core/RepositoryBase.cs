using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;

namespace CalculateFunding.Services.Core
{
    public abstract class RepositoryBase : IHealthChecker
    {
        protected readonly ICosmosRepository _cosmosRepository;

        protected RepositoryBase(ICosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            var health = new ServiceHealth
            {
                Name = GetType().Name
            };
            (bool Ok, string Message) = _cosmosRepository.IsHealthOk();
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = GetType().Name, Message = Message });

            return Task.FromResult(health);
        }
    }
}