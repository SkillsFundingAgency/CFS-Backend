using CalculateFunding.Models.Health;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Results.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class PublishedProviderCalculationResultsRepository : IPublishedProviderCalculationResultsRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;

        public PublishedProviderCalculationResultsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var cosmosRepoHealth = await _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishedProviderCalculationResultsRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
        }

        public Task CreatePublishedCalculationResults(IEnumerable<PublishedProviderCalculationResult> publishedCalculationResults)
        {
            Guard.ArgumentNotNull(publishedCalculationResults, nameof(publishedCalculationResults));

            return _cosmosRepository.BulkCreateAsync(publishedCalculationResults.Select(m => new KeyValuePair<string, PublishedProviderCalculationResult>(m.ProviderId, m)));
        }

        public Task<IEnumerable<PublishedProviderCalculationResultHistory>> GetPublishedProviderCalculationHistoryForSpecificationId(string specificationId)
        {
            IQueryable<PublishedProviderCalculationResultHistory> results = _cosmosRepository.Query<PublishedProviderCalculationResultHistory>(enableCrossPartitionQuery: true).Where(m => m.SpecificationId == specificationId);

            return Task.FromResult(results.AsEnumerable());
        }

        public Task SavePublishedCalculationResultsHistory(IEnumerable<PublishedProviderCalculationResultHistory> publishedCalculationResultsHistory)
        {
            Guard.ArgumentNotNull(publishedCalculationResultsHistory, nameof(publishedCalculationResultsHistory));

            return _cosmosRepository.BulkCreateAsync<PublishedProviderCalculationResultHistory>(publishedCalculationResultsHistory.Select(m => new KeyValuePair<string, PublishedProviderCalculationResultHistory>(m.ProviderId, m)));
        }
    }
}
