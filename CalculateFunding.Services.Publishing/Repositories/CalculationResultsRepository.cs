using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(CalculationResultsRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = Message });

            return health;
        }
        public Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId, int maxItemCount = -1)
        {
            List<ProviderResult> results;
            if (maxItemCount > 0)
            {
                results = _cosmosRepository.Query<ProviderResult>(enableCrossPartitionQuery: true).Where(x => x.SpecificationId == specificationId).Take(maxItemCount).ToList();
            }
            else
            {
                results = _cosmosRepository.Query<ProviderResult>(enableCrossPartitionQuery: true).Where(x => x.SpecificationId == specificationId).ToList();
            }

            return Task.FromResult(results.AsEnumerable());
        }
    }
}
